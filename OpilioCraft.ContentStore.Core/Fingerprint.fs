[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OpilioCraft.ContentStore.Core.Fingerprint

open System.IO
open System.Security.Cryptography
open System.Text.RegularExpressions

// Algorithm used
let private hashingAlgorithm = SHA256.Create();

// Helper functions
let private readStream (stream : FileStream) =
    seq {
        let mutable currentByte = 0

        let moveNext () =
            currentByte <- stream.ReadByte()
            currentByte >= 0

        while moveNext () do
            yield byte currentByte
    }

let private readBytes filename length =
    use stream = File.OpenRead(filename)
    readStream stream |> Seq.truncate length |> Seq.toArray

let private convertBytesToString (bytes : byte[]) =
    bytes |> Array.fold (fun resultString b -> resultString + b.ToString("x2")) ""
    
// Fingerprint calculations
let calculatePartlyFingerprint filename =
    hashingAlgorithm.ComputeHash(readBytes filename 1024)

let calculateFingerprint filename =
    use stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16 * 1024 * 1024)
    hashingAlgorithm.ComputeHash(stream)

// Converter functions
let fingerprintAsString = calculateFingerprint >> convertBytesToString
let partlyFingerprintAsString = calculatePartlyFingerprint >> convertBytesToString

// Typed result
let getFullFingerprint filename = filename |> fingerprintAsString |> QualifiedFingerprint.Full
let getPartlyFingerprint filename = filename |> partlyFingerprintAsString |> QualifiedFingerprint.Partly

let private _fingerprintRegex = Regex(@".#([0-9a-z]{64})$", RegexOptions.Compiled)

let getFingerprint (filename : string) =
    let matchResult =
        filename
        |> Path.GetFileNameWithoutExtension
        |> _fingerprintRegex.Match

    if matchResult.Success then
        matchResult.Groups.[1].Value |> QualifiedFingerprint.Derived
    else
        filename |> getFullFingerprint
