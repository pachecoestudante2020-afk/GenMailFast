# GenMailFast

Fast .NET 8 console tool for generating email candidates from names/usernames and writing local output files.

## Build

```bash
dotnet restore
dotnet build GenMailFast.sln -c Release
```

## Run

```bash
dotnet run --project src/GenMailFast/GenMailFast.csproj -- --input names.txt --domain example.com --out output --rows-per-file 50000 --dedupe dedupe/dedupe.hashes.gz
```

## Arguments

- `--input <path>` (required)
- `--domain <domain>` (required)
- `--out <folder>` (default: `output`)
- `--dedupe <path>` (default: `dedupe/dedupe.hashes.gz`)
- `--rows-per-file <number>` (default: `50000`)
- `--compress-output true|false` (default: `false`)
- `--rules <csv>` (default: `firstlast,first.dot.last,all,all.dot`)
- `--numbers none|suffix` (default: `none`)
- `--number-range <range>`

## Dedupe behavior

- Dedupe uses `dedupe.hashes.gz` with binary header + version + FNV-1a 64-bit hashes.
- Hash collisions are theoretically possible but extremely unlikely in practical use.
- Existing hashes are loaded at startup and saved atomically at the end (`.tmp` then replace).
- Reset dedupe by deleting the dedupe file.

## Output splitting

Split files reduce memory pressure and make downstream processing easier. Each file has at most `--rows-per-file` lines:

- Plain text: `emails_001.txt`, `emails_002.txt`, ...
- Compressed: `emails_001.txt.gz`, `emails_002.txt.gz`, ...

Enable compressed output with `--compress-output true`.

## Performance notes

- Streams input line by line.
- Streams output with 4MB buffers.
- Uses `HashSet<ulong>` for fast dedupe checks.
- Avoids regex in hot path.
- Progress logs every 100,000 lines or every 2 seconds.

## Safety

This tool does **not** send mail, verify mailboxes, use SMTP, scrape websites, use proxies, bypass captcha, or provide bulk messaging.
