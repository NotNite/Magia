# Magia

Code modding with flair. A set of C# libraries for helping interact with native code.

## Magia.Signatures

A signature scanning library, based off of [pelite's pattern format](https://docs.rs/pelite/latest/pelite/pattern/fn.parse.html).

Assumes you are injected into the target process and the target process is a x64 Windows application. Some features assume compiler quirks (e.g. MSVC function padding with `0xCC`). Signatures can be assembled from their class forms, or parsed from a string. The following commands are available:

- Whitespace is ignored. Comments (`#`) ignore the rest of the line.
- Match a literal byte by typing its two-letter hex representation (e.g. `E8`).
- Match displacements/offsets using the characters `%` (1 byte), `&` (2 byte), and `$` (4 byte).
  - A `call` instruction can be represented as `E8 $`.
- Match 8-byte absolute pointers with the `*` character.
- Follow displacements with the `{ }` operator.
  - For example, `E8 $ { more stuff here }` will match a function that calls the function matched inside of the braces.
- Execute multiple possible subpatterns with the `( )` operator.
  - Separate subpatterns with `|`.
- Skip a byte with `??`, or skip multiple with `[ ]`.
  - `??` for one byte, `[number]` for multiple (e.g. `[4]`).
  - Square brackets support ranges (e.g. `[4-8]`).
- Skip an unknown number of bytes with `@`.
  - This will repeat a search until it encounters a `CC`, which MSVC uses to pad functions.
- Save to the return array with `\`.
  - Loads are currently unimplemented, but the `startOverride` argument can be passed to `SignatureScanner#Scan` to start the search at a specific address.
- Match UTF-8 string literals with quotes (e.g. `"Hello, world!"`).
