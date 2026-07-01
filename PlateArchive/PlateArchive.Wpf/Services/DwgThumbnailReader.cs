using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PlateArchive.Wpf.Services;

/// <summary>
/// Estrae l'anteprima di un file DWG senza dipendenze esterne.
///
/// Strategia primaria  : Windows Shell thumbnail API — identica ad Explorer,
///                       funziona se AutoCAD (o qualsiasi provider DWG) è installato.
/// Strategia di riserva: parsing embedded preview nel binario DWG:
///   - Sentinel 1F25…  → BMP/DIB (AC1012-AC1021)
///   - Firma PNG        → (AC1024+, R2010 in poi)
///   - Firma JPEG       → fallback
/// </summary>
public static class DwgThumbnailReader
{
    private static readonly byte[] _sentinel =
        [0x1F, 0x25, 0x03, 0xD5, 0x4F, 0x19, 0xA2, 0xB2,
         0xD6, 0x4E, 0x6B, 0x31, 0xDD, 0xA7, 0xC2, 0x3C];

    private static readonly byte[] _pngSig  = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] _jpegSig = [0xFF, 0xD8, 0xFF];

    // ──────────────────────────────────────────────────────────────────────────

    public static async Task<BitmapSource?> EstraiAnteprimaAsync(string? percorsoFile)
    {
        if (string.IsNullOrWhiteSpace(percorsoFile)) return null;
        if (!percorsoFile.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase)) return null;
        if (!File.Exists(percorsoFile)) return null;

        return await Task.Run(() =>
        {
            // ── 1. Shell thumbnail (AutoCAD installato) ───────────────────────
            var result = GetShellThumbnail(percorsoFile, 480);
            if (result is not null) return result;

            // ── 2. Embedded preview nel file DWG ─────────────────────────────
            return LeggiEmbedded(percorsoFile);
        });
    }

    // ── Shell thumbnail ───────────────────────────────────────────────────────

    /// <summary>
    /// Chiede al Windows Shell il thumbnail tramite IShellItemImageFactory.
    /// Deve girare su thread STA (requisito COM) — ne crea uno dedicato.
    /// </summary>
    private static BitmapSource? GetShellThumbnail(string path, int pixelSize)
    {
        BitmapSource? result = null;

        var sta = new Thread(() =>
        {
            try
            {
                var iid = NativeMethods.IID_IShellItemImageFactory;
                int hr  = NativeMethods.SHCreateItemFromParsingName(
                    path, IntPtr.Zero, ref iid, out var factory);
                if (hr != 0 || factory is null) return;

                var sz = new NativeMethods.SIZE { cx = pixelSize, cy = pixelSize };
                hr = factory.GetImage(sz, NativeMethods.SIIGBF.ThumbnailOnly, out var hBmp);
                if (hr != 0 || hBmp == IntPtr.Zero) return;

                try
                {
                    var full = Imaging.CreateBitmapSourceFromHBitmap(
                        hBmp, IntPtr.Zero, Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    var cropped = RitagliaPadding(full);
                    cropped.Freeze();
                    result = cropped;
                }
                finally { NativeMethods.DeleteObject(hBmp); }
            }
            catch { }
        });

        sta.SetApartmentState(ApartmentState.STA);
        sta.IsBackground = true;
        sta.Start();
        sta.Join(TimeSpan.FromSeconds(10)); // timeout di sicurezza
        return result;
    }

    // ── Embedded preview ──────────────────────────────────────────────────────

    private static BitmapSource? LeggiEmbedded(string percorsoFile)
    {
        try
        {
            using var fs = new FileStream(percorsoFile,
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length < 32) return null;

            using var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: true);
            var version = Encoding.ASCII.GetString(br.ReadBytes(6));
            if (!version.StartsWith("AC", StringComparison.Ordinal)) return null;

            fs.Seek(0, SeekOrigin.Begin);
            int bufLen = (int)Math.Min(fs.Length, 4 * 1024 * 1024);
            var buf    = new byte[bufLen];
            _ = fs.Read(buf, 0, bufLen);

            BitmapSource? result = null;

            // Offset diretto a 0x0D (AC1012-AC1015)
            if (bufLen >= 17)
            {
                int ofs = BitConverter.ToInt32(buf, 0x0D);
                if (ofs > 0 && ofs + 16 < bufLen && BytesAt(buf, ofs, _sentinel))
                    result = ParseSentinel(buf, ofs + 16);
            }

            // Scansione sentinel (AC1018-AC1021)
            if (result is null)
            {
                int si = 0;
                while ((si = TrovaBytes(buf, _sentinel, si)) >= 0)
                {
                    result = ParseSentinel(buf, si + 16);
                    if (result is not null) break;
                    si++;
                }
            }

            // PNG (AC1024+)
            if (result is null)
            {
                int si = 0;
                while ((si = TrovaBytes(buf, _pngSig, si)) >= 0)
                {
                    result = DecodaSlice(buf, si);
                    if (result is not null) break;
                    si++;
                }
            }

            // JPEG
            if (result is null)
            {
                int si = 0;
                while ((si = TrovaBytes(buf, _jpegSig, si)) >= 0)
                {
                    result = DecodaSlice(buf, si);
                    if (result is not null) break;
                    si++;
                }
            }

            result?.Freeze();
            return result;
        }
        catch { return null; }
    }

    private static BitmapSource? ParseSentinel(byte[] buf, int pos)
    {
        try
        {
            if (pos + 5 > buf.Length) return null;

            pos += 4; // OverallSize
            int count = buf[pos++];
            if (count == 0 || pos + count * 9 > buf.Length) return null;

            int hdrOffset = -1, hdrSize = 0;
            int dibOffset = -1, dibSize = 0;

            for (int i = 0; i < count; i++)
            {
                byte code   = buf[pos];
                int  offset = BitConverter.ToInt32(buf, pos + 1);
                int  size   = BitConverter.ToInt32(buf, pos + 5);
                pos += 9;

                // Code 1 = BITMAPFILEHEADER, Code 2 = DIB data
                // Code 3 = WMF — non supportato senza System.Drawing
                if (code == 1) { hdrOffset = offset; hdrSize = size; }
                if (code == 2) { dibOffset = offset; dibSize = size; }
            }

            if (dibOffset < 0 || dibSize <= 0 || dibOffset + dibSize > buf.Length) return null;

            byte[]? bmpData;
            if (hdrOffset >= 0 && hdrSize > 0 && hdrOffset + hdrSize <= buf.Length)
            {
                bmpData = new byte[hdrSize + dibSize];
                Buffer.BlockCopy(buf, hdrOffset, bmpData, 0,       hdrSize);
                Buffer.BlockCopy(buf, dibOffset, bmpData, hdrSize, dibSize);
            }
            else
            {
                bmpData = CostruisciBmp(buf, dibOffset, dibSize);
                if (bmpData is null) return null;
            }

            return DecodaSlice(bmpData, 0);
        }
        catch { return null; }
    }

    private static byte[]? CostruisciBmp(byte[] buf, int dibOffset, int dibSize)
    {
        if (dibSize < 40) return null;
        int biSize     = BitConverter.ToInt32(buf, dibOffset);
        int biBitCount = BitConverter.ToInt16(buf, dibOffset + 14);
        int biClrUsed  = BitConverter.ToInt32(buf, dibOffset + 32);
        int palette    = biClrUsed > 0 ? biClrUsed :
            biBitCount switch { 1 => 2, 4 => 16, 8 => 256, _ => 0 };
        int bfOffBits  = 14 + biSize + palette * 4;
        int bfSize     = 14 + dibSize;
        var r = new byte[bfSize];
        r[0] = (byte)'B'; r[1] = (byte)'M';
        Buffer.BlockCopy(BitConverter.GetBytes(bfSize),    0, r, 2,  4);
        Buffer.BlockCopy(BitConverter.GetBytes(bfOffBits), 0, r, 10, 4);
        Buffer.BlockCopy(buf, dibOffset, r, 14, dibSize);
        return r;
    }

    private static BitmapSource? DecodaSlice(byte[] buf, int offset)
    {
        int length = buf.Length - offset;
        if (length <= 0) return null;
        try
        {
            using var ms = new MemoryStream(buf, offset, length, writable: false);
            var dec = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            return dec.Frames.Count > 0 ? dec.Frames[0] : null;
        }
        catch { return null; }
    }

    // ── Utilità ───────────────────────────────────────────────────────────────

    private static bool BytesAt(byte[] buf, int pos, byte[] pat)
    {
        if (pos < 0 || pos + pat.Length > buf.Length) return false;
        for (int i = 0; i < pat.Length; i++)
            if (buf[pos + i] != pat[i]) return false;
        return true;
    }

    private static int TrovaBytes(byte[] hay, byte[] needle, int from = 0)
    {
        int n = needle.Length;
        if (n == 0) return from;
        int limit = hay.Length - n;
        if (from > limit) return -1;
        var skip = new int[256];
        for (int i = 0; i < 256; i++) skip[i] = n;
        for (int i = 0; i < n - 1; i++) skip[needle[i]] = n - 1 - i;
        int k = from + n - 1;
        while (k < hay.Length)
        {
            int j = n - 1, i = k;
            while (j >= 0 && hay[i] == needle[j]) { i--; j--; }
            if (j < 0) return i + 1;
            k += Math.Max(1, skip[hay[k]]);
        }
        return -1;
    }

    // ── Auto-crop padding uniforme ────────────────────────────────────────────

    /// <summary>
    /// Rimuove il padding a colore uniforme (solitamente bianco aggiunto dalla Shell)
    /// dai quattro lati dell'immagine. Usa il pixel in basso a destra come colore
    /// di riferimento del background, con tolleranza ±20 per canale.
    /// </summary>
    private static BitmapSource RitagliaPadding(BitmapSource src)
    {
        try
        {
            var bgra = new FormatConvertedBitmap();
            bgra.BeginInit();
            bgra.Source            = src;
            bgra.DestinationFormat = PixelFormats.Bgra32;
            bgra.EndInit();

            int w = bgra.PixelWidth, h = bgra.PixelHeight;
            if (w < 4 || h < 4) return src;

            int stride = w * 4;
            var px = new byte[h * stride];
            bgra.CopyPixels(px, stride, 0);

            // Colore background: pixel in basso a destra (padding Shell = bianco)
            int brIdx = ((h - 1) * w + (w - 1)) * 4;
            byte bgB = px[brIdx], bgG = px[brIdx + 1], bgR = px[brIdx + 2];
            const int T = 20;

            bool IsBg(int idx) =>
                Math.Abs(px[idx]     - bgB) <= T &&
                Math.Abs(px[idx + 1] - bgG) <= T &&
                Math.Abs(px[idx + 2] - bgR) <= T;

            bool IsRowBg(int r) { int s = r * stride; for (int x = 0; x < w; x++) if (!IsBg(s + x * 4)) return false; return true; }
            bool IsColBg(int c) {                      for (int y = 0; y < h; y++) if (!IsBg(y * stride + c * 4)) return false; return true; }

            int top = 0, bot = h - 1, lft = 0, rgt = w - 1;
            while (top < bot  && IsRowBg(top)) top++;
            while (bot > top  && IsRowBg(bot)) bot--;
            while (lft < rgt  && IsColBg(lft)) lft++;
            while (rgt > lft  && IsColBg(rgt)) rgt--;

            int cw = rgt - lft + 1, ch = bot - top + 1;
            if (cw == w && ch == h) return src; // nessun padding rilevato

            // Copia i pixel dell'area ritagliata in un nuovo BitmapSource
            var cropped = new byte[ch * cw * 4];
            for (int row = 0; row < ch; row++)
                Buffer.BlockCopy(px, (top + row) * stride + lft * 4,
                                 cropped, row * cw * 4, cw * 4);

            return BitmapSource.Create(cw, ch, src.DpiX, src.DpiY,
                                       PixelFormats.Bgra32, null, cropped, cw * 4);
        }
        catch { return src; }
    }

    // ── P/Invoke Shell ────────────────────────────────────────────────────────

    private static class NativeMethods
    {
        internal static Guid IID_IShellItemImageFactory =
            new("BCC18B79-BA16-442F-80C4-8A59C30C463B");

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            ref Guid riid,
            out IShellItemImageFactory ppv);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
        internal interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(SIZE size, SIIGBF flags, out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SIZE { public int cx, cy; }

        [Flags]
        internal enum SIIGBF
        {
            ResizeToFit   = 0x0,
            BiggerSizeOk  = 0x1,
            MemoryOnly    = 0x2,
            IconOnly      = 0x4,
            ThumbnailOnly = 0x8,
            InCacheOnly   = 0x10,
        }
    }
}
