namespace PlateArchive.Services;

/// <summary>
/// Riga di ordine di vendita non evasa, letta dal gestionale (DB2/Panthera).
/// Sola lettura: non ha una controparte locale nel database di PlateArchive.
/// <para>
/// <see cref="Valori"/> contiene TUTTE le colonne della query configurata in appsettings.json
/// (Db2:QueryRigheOrdineVendita), nell'ordine della SELECT: è la query a comandare le colonne
/// mostrate in griglia — per aggiungere/togliere/rinominare una colonna basta modificare la
/// SELECT (un alias, es. <c>a.DESCR_ESTESA AS "Descrizione estesa"</c>, cambia l'intestazione).
/// Tutto viene letto come stringa: le colonne DB2/AS400 hanno tipi eterogenei, e alcune
/// CHARACTER(10) contengono causale e numero concatenati (es. <c>"VS  003071"</c>).
/// </para>
/// <see cref="CodiceArticolo"/>, <see cref="CodiceClienteGestionale"/> e
/// <see cref="DescrizioneArticolo"/> sono estratti per NOME dalle colonne <c>R_ARTICOLO</c>,
/// <c>R_CLIENTE</c> e <c>DESCR_ESTESA</c> (che quindi devono restare nella SELECT senza alias)
/// e alimentano la logica applicativa: ricerca piastra per codice articolo, auto-associazione
/// cliente↔piastra, descrizione articolo nei dialog Associa piastra / Caratteristiche piastra.
/// </summary>
public record RigaOrdineVendita(
    string CodiceArticolo,
    string CodiceClienteGestionale,
    string DescrizioneArticolo,
    IReadOnlyList<string> Valori);

/// <summary>
/// Esito della lettura righe ordine: nomi colonna della query (diventano le intestazioni
/// della griglia) + righe, con i valori allineati per posizione a <see cref="Colonne"/>.
/// </summary>
public record RigheOrdineVenditaResult(
    IReadOnlyList<string>            Colonne,
    IReadOnlyList<RigaOrdineVendita> Righe);
