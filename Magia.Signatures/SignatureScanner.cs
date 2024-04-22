using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Magia.Signatures;

public class SignatureScanner(ProcessModule module) {
    private nint start = module.BaseAddress;
    private nint end = module.BaseAddress + module.ModuleMemorySize;

    public List<nint>? Scan(SignatureCommand[] signature, nint? startOverride = null) {
        var results = new List<nint>();
        var i = this.start;

        if (startOverride != null) {
            return this.TryScan(startOverride.Value, signature, results) ? results : null;
        }

        while (i < end) {
            results.Clear();
            if (this.TryScan(i, signature, results)) return results;
            i++;
        }

        return null;
    }

    private bool TryScan(nint pos, SignatureCommand[] signature, List<nint> results) {
        if (signature.Length == 0) return true;

        var i = 0;

        while (i < signature.Length) {
            var command = signature[i];

            if (command is BytesCommand bytes) {
                if (pos + bytes.Bytes.Length > this.end) return false;

                var actualBytes = new byte[bytes.Bytes.Length];
                Marshal.Copy(pos, actualBytes, 0, actualBytes.Length);
                if (!actualBytes.SequenceEqual(bytes.Bytes)) return false;

                pos += bytes.Bytes.Length;
                i++;
            }

            if (command is SkipCommand skip) {
                pos += skip.Size;
                i++;
            }

            if (command is SkipRangeCommand skipRange) {
                var afterThis = signature.Skip(i + 1).ToArray();
                for (var j = skipRange.Min; j <= skipRange.Max; j++) {
                    if (this.TryScan(pos + j, afterThis, results)) {
                        return true;
                    }
                }

                return false;
            }

            if (command is SkipUnknownCommand) {
                var afterThis = signature.Skip(i + 1).ToArray();
                while (Marshal.ReadByte(pos) != 0xCC && pos < this.end) {
                    if (this.TryScan(pos, afterThis, results)) return true;
                    pos++;
                }

                return false;
            }

            if (command is SaveCommand save) {
                if (results.Count <= save.Index)
                    results.AddRange(Enumerable.Repeat<nint>(0, save.Index - results.Count + 1));
                results[save.Index] = pos;
                i++;
            }

            if (command is LoadCommand load) {
                pos = load.Value;
                i++;
            }

            if (command is DisplacementCommand displacement) {
                var offset = displacement.Size switch {
                    1 => Marshal.ReadByte(pos),
                    2 => Marshal.ReadInt16(pos),
                    4 => Marshal.ReadInt32(pos),
                    _ => throw new ArgumentOutOfRangeException()
                };
                pos += displacement.Size + offset;
                if (pos < this.start || pos >= this.end) return false;
                i++;
            }

            if (command is PointerCommand) {
                pos = Marshal.ReadIntPtr(pos);
                i++;
            }

            if (command is OneOfCommand oneOf) {
                var after = signature.Skip(i + 1).ToArray();
                foreach (var commands in oneOf.Commands) {
                    if (this.TryScan(pos, commands, results)) {
                        if (this.TryScan(pos, after, results)) {
                            return true;
                        }
                    }
                }

                return false;
            }

            if (command is FollowCommand follow) {
                if (this.TryScan(pos, follow.Commands, results)) {
                    i++;
                } else {
                    return false;
                }
            }
        }

        return true;
    }
}
