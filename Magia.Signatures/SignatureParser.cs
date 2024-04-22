using System.Globalization;
using System.Text;

namespace Magia.Signatures;

public static class SignatureParser {
    public static SignatureCommand[] Parse(string sig, int save = 0, bool entry = true) {
        var idx = 0;
        var result = new List<SignatureCommand>();

        // Implicit saves at the start of every sig
        if (entry) result.Add(new SaveCommand {Index = 0});

        while (idx < sig.Length) {
            var cmd = sig[idx];

            // Parse a byte
            if (char.IsAsciiHexDigit(cmd)) {
                var @byte = byte.Parse(sig.Substring(idx, 2), NumberStyles.HexNumber);

                // Merge bytes
                if (result.Count >= 1 && result[^1] is BytesCommand bytes) {
                    var newBytes = new List<byte>(bytes.Bytes) {@byte};
                    result[^1] = new BytesCommand {Bytes = newBytes.ToArray()};
                } else {
                    result.Add(new BytesCommand {Bytes = [@byte]});
                }

                idx += 2;
                continue;
            }

            switch (cmd) {
                // Ignore rest of line with comments
                case '#': {
                    while (idx < sig.Length && sig[idx] != '\n') idx++;
                    break;
                }

                // Displacements and pointers
                case '%': {
                    result.Add(new DisplacementCommand {Size = 1});
                    idx++;
                    break;
                }
                case '&': {
                    result.Add(new DisplacementCommand {Size = 2});
                    idx++;
                    break;
                }
                case '$': {
                    result.Add(new DisplacementCommand {Size = 4});
                    idx++;
                    break;
                }
                case '*': {
                    result.Add(new PointerCommand());
                    idx++;
                    break;
                }

                // Follow a displacement
                case '{': {
                    var str = GetContained(sig, idx, '{', '}');
                    var parsed = Parse(str, save, false);
                    result.Add(new FollowCommand {Commands = parsed});
                    idx += str.Length + 2;
                    break;
                }

                // Subpattern
                case '(': {
                    var str = GetContained(sig, idx, '(', ')');
                    var patterns = str
                        .Split('|')
                        .Select(x => Parse(x, save, false)).ToArray();
                    result.Add(new OneOfCommand {Commands = patterns});
                    idx += str.Length + 2;
                    break;
                }

                // Skip or skip many
                case '?': {
                    // Next char must be a question mark
                    if (idx >= sig.Length || sig[idx] != '?')
                        throw new Exception("Invalid skip operator");

                    result.Add(new SkipCommand {Size = 1});
                    idx += 2;
                    break;
                }

                case '[': {
                    var str = GetContained(sig, idx, '[', ']');

                    if (str.Contains('-')) {
                        // Skip many
                        var parts = str.Split('-');
                        if (parts.Length != 2) throw new Exception("Invalid many operator");
                        if (!int.TryParse(parts[0], out var min) || !int.TryParse(parts[1], out var max))
                            throw new Exception("Invalid many operator");
                        result.Add(new SkipRangeCommand {Min = min, Max = max});
                    } else {
                        // Skip
                        if (!int.TryParse(str, out var size)) throw new Exception("Invalid skip operator");
                        result.Add(new SkipCommand {Size = size});
                    }

                    idx += str.Length + 2;
                    break;
                }
                case '@': {
                    result.Add(new SkipUnknownCommand());
                    idx++;
                    break;
                }

                // Save and load
                case '\\': {
                    result.Add(new SaveCommand {Index = save});
                    save++;
                    idx++;
                    break;
                }
                case '/': {
                    result.Add(new LoadCommand {Value = save});
                    idx++;
                    break;
                }

                // Match string
                case '"': {
                    var start = idx + 1;
                    var end = sig.IndexOf('"', start);
                    var str = sig.Substring(start, end - start);

                    var bytes = Encoding.UTF8.GetBytes(str);
                    result.Add(new BytesCommand {Bytes = bytes});

                    idx += str.Length + 2;
                    break;
                }

                default: {
                    idx++;
                    break;
                }
            }
        }

        return result.ToArray();
    }

    private static string GetContained(string str, int start, char open, char close) {
        var end = start;
        var left = 1;
        while (left > 0) {
            end++;
            if (str[end] == open) left++;
            if (str[end] == close) left--;
        }
        return str.Substring(start + 1, end - start - 1);
    }
}
