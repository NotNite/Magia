namespace Magia.Signatures;

public abstract record SignatureCommand;

/// Scan for a certain amount of bytes.
public record BytesCommand : SignatureCommand {
    public required byte[] Bytes;
}

/// Skip a certain amount of bytes.
public record SkipCommand : SignatureCommand {
    public required int Size;
}

/// Skip a range of bytes.
public record SkipRangeCommand : SignatureCommand {
    public required int Min;
    public required int Max;
}

/// Skip an unknown amount of bytes, stopping if another function is encountered.
public record SkipUnknownCommand : SignatureCommand;

/// Save the current address to the results.
public record SaveCommand : SignatureCommand {
    public required int Index;
}

/// Jump to the specified address.
public record LoadCommand : SignatureCommand {
    public required nint Value;
}

/// Read <see cref="Size"/> bytes, using it as an offset and jumping to it.
public record DisplacementCommand : SignatureCommand {
    public required int Size;
}

/// Read 8 bytes, using it as an absolute address and jumping to it.
public record PointerCommand : SignatureCommand;

/// Execute all options in the <see cref="Commands"/> array, using the first one that succeeds.
public record OneOfCommand : SignatureCommand {
    public required SignatureCommand[][] Commands;
}

/// Follow the last displacement, executing this signature, then returning to the original address.
public record FollowCommand : SignatureCommand {
    public required SignatureCommand[] Commands;
}
