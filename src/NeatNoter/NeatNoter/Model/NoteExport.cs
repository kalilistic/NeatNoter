using System.Collections.Generic;

namespace NeatNoter;

public class NoteExport
{
    public string Name { get; set; } = string.Empty;

    public int Id { get; set; }

    public string Body { get; set; } = string.Empty;

    public long Created { get; set; }

    public long Modified { get; set; }

    public string Categories { get; set; } = string.Empty;
}
