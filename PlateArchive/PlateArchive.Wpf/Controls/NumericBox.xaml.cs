using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlateArchive.Wpf.Controls;

public partial class NumericBox : UserControl
{
    // ── DependencyProperties ──────────────────────────────────────────────────

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value), typeof(decimal?), typeof(NumericBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValueChanged));

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(nameof(Step), typeof(decimal), typeof(NumericBox),
            new PropertyMetadata(1m));

    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(NumericBox),
            new PropertyMetadata(2));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(decimal?), typeof(NumericBox),
            new PropertyMetadata(null));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(decimal?), typeof(NumericBox),
            new PropertyMetadata(null));

    // ── Proprietà CLR ─────────────────────────────────────────────────────────

    public decimal? Value
    {
        get => (decimal?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public decimal Step
    {
        get => (decimal)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public int DecimalPlaces
    {
        get => (int)GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    public decimal? Minimum
    {
        get => (decimal?)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public decimal? Maximum
    {
        get => (decimal?)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    // ── Stato interno ─────────────────────────────────────────────────────────

    // Impedisce loop: aggiornamento esterno Value → testo → Value → ...
    private bool _updatingFromValue;
    // Impedisce loop: testo dell'utente → Value → callback → riscrittura testo
    private bool _updatingFromText;

    // ── Costruttore ───────────────────────────────────────────────────────────

    public NumericBox()
    {
        InitializeComponent();
        ValueTextBox.TextChanged       += TextBox_TextChanged;
        ValueTextBox.LostFocus         += TextBox_LostFocus;
        ValueTextBox.GotFocus          += TextBox_GotFocus;
        ValueTextBox.KeyDown           += TextBox_KeyDown;
        ValueTextBox.PreviewMouseWheel += TextBox_MouseWheel;
        IncrementBtn.Click             += (_, _) => Increment();
        DecrementBtn.Click             += (_, _) => Decrement();
    }

    // ── Callback cambio Value (dal binding esterno) ───────────────────────────

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (NumericBox)d;
        // Aggiorna il testo solo se il cambio viene dall'esterno (non dall'utente che digita)
        if (!ctrl._updatingFromText)
            ctrl.ApplicaValoreAlTesto();
    }

    // ── Scrittura del testo dalla proprietà Value ──────────────────────────────

    private void ApplicaValoreAlTesto()
    {
        _updatingFromValue = true;
        var fmt = "F" + DecimalPlaces;
        ValueTextBox.Text = Value.HasValue
            ? Value.Value.ToString(fmt, CultureInfo.InvariantCulture)
            : string.Empty;
        _updatingFromValue = false;
    }

    // ── Gestori TextBox ───────────────────────────────────────────────────────

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingFromValue) return;

        var testo = ValueTextBox.Text.Replace(',', '.');
        if (decimal.TryParse(testo, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
        {
            // L'utente ha scritto un numero valido: aggiorna Value senza riscrivere il testo
            _updatingFromText = true;
            Value = Clamp(v);
            _updatingFromText = false;
        }
        // Se il testo non è ancora parseable (es. "1." o "-"), non fare nulla:
        // l'utente sta ancora digitando.
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // Alla perdita del focus normalizza: valida e formatta
        var testo = ValueTextBox.Text.Trim().Replace(',', '.');
        Value = string.IsNullOrEmpty(testo)
            ? null
            : decimal.TryParse(testo, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                ? Clamp(v)
                : null;
        ApplicaValoreAlTesto();
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        // Seleziona tutto al focus per facilitare la sostituzione rapida del valore
        ValueTextBox.SelectAll();
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up)   { Increment(); e.Handled = true; }
        if (e.Key == Key.Down) { Decrement(); e.Handled = true; }
    }

    private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!ValueTextBox.IsKeyboardFocused) return;
        if (e.Delta > 0) Increment();
        else             Decrement();
        e.Handled = true;
    }

    // ── Incremento / decremento ───────────────────────────────────────────────

    private void Increment()
    {
        Value = Clamp((Value ?? 0m) + Step);
        ApplicaValoreAlTesto();
    }

    private void Decrement()
    {
        Value = Clamp((Value ?? 0m) - Step);
        ApplicaValoreAlTesto();
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private decimal Clamp(decimal v)
    {
        if (Minimum.HasValue && v < Minimum.Value) return Minimum.Value;
        if (Maximum.HasValue && v > Maximum.Value) return Maximum.Value;
        return v;
    }
}
