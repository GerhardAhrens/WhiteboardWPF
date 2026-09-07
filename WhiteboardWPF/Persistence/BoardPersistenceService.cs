namespace WhiteboardWPF.Persistence
{
    using System.IO;
    using System.Text.Json;

    using WhiteboardWPF.Models;

    /// <summary>
    /// Liest und schreibt Whiteboard-Dokumente im JSON-Format.
    /// Enthält keine WPF-UI-Logik.
    /// </summary>
    public sealed class BoardPersistenceService
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public void Save(string fileName, WhiteBoardDocument document)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Ein Dateiname muss angegeben werden.", nameof(fileName));

            if (document == null)
                throw new ArgumentNullException(nameof(document));

            string json = JsonSerializer.Serialize(document, _jsonOptions);
            File.WriteAllText(fileName, json);
        }

        public WhiteBoardDocument Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Ein Dateiname muss angegeben werden.", nameof(fileName));

            string json = File.ReadAllText(fileName);

            WhiteBoardDocument? document =
                JsonSerializer.Deserialize<WhiteBoardDocument>(json, _jsonOptions);

            if (document == null)
                throw new InvalidOperationException("Die Whiteboard-Datei konnte nicht gelesen werden.");

            return document;
        }
    }
}
