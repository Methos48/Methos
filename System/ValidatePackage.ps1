param([Parameter(Mandatory=$true)][string]$PackagePath)
$ErrorActionPreference = 'Stop'
$data = [IO.File]::ReadAllBytes($PackagePath)
if ($data.Length -lt 64 -or [BitConverter]::ToUInt32($data,0) -ne 2653586369) { throw 'Pacote Unreal invalido.' }
$stream = [IO.MemoryStream]::new($data,$false)
$reader = [IO.BinaryReader]::new($stream)
function Read-Compact {
    $b=$reader.ReadByte(); $negative=($b -band 128) -ne 0
    $value=[int]($b -band 63); $more=($b -band 64) -ne 0; $shift=6
    while($more) {
        $b=$reader.ReadByte(); $value=$value -bor (($b -band 127) -shl $shift)
        $more=($b -band 128) -ne 0; $shift+=7
        if($shift -gt 34){throw 'Indice compacto invalido.'}
    }
    if($negative){return -$value}; return $value
}
try {
    $nameCount=[BitConverter]::ToInt32($data,12)
    $nameOffset=[BitConverter]::ToInt32($data,16)
    $exportCount=[BitConverter]::ToInt32($data,20)
    $exportOffset=[BitConverter]::ToInt32($data,24)
    if($nameCount -le 0 -or $nameCount -gt $data.Length -or $exportCount -le 0 -or $exportCount -gt $data.Length){throw 'Contagens invalidas.'}
    if($nameOffset -lt 36 -or $nameOffset -ge $data.Length -or $exportOffset -lt 36 -or $exportOffset -ge $data.Length){throw 'Offsets invalidos.'}
    $names=[Collections.Generic.List[string]]::new()
    $stream.Position=$nameOffset
    for($i=0;$i -lt $nameCount;$i++) {
        $length=Read-Compact
        $byteCount=[Math]::Abs($length)
        if($length -lt 0){$byteCount*=2}
        if($byteCount -le 0 -or $byteCount -gt $data.Length-$stream.Position){throw 'Nome truncado.'}
        $raw=$reader.ReadBytes($byteCount)
        if($length -lt 0){$name=[Text.Encoding]::Unicode.GetString($raw,0,$raw.Length-2)}
        else{$name=[Text.Encoding]::GetEncoding(1252).GetString($raw,0,$raw.Length-1)}
        $names.Add($name)
        [void]$reader.ReadUInt32()
    }
    $stream.Position=$exportOffset
    $publicCount=0
    for($i=0;$i -lt $exportCount;$i++) {
        [void](Read-Compact); [void](Read-Compact); [void]$reader.ReadInt32()
        $nameIndex=Read-Compact
        $flags=$reader.ReadUInt32()
        $size=Read-Compact
        if($size -lt 0){throw 'Tamanho de exportacao invalido.'}
        if($size -gt 0){$offset=Read-Compact; if($offset -lt 36 -or [long]$offset+$size -gt $data.Length){throw 'Exportacao fora do arquivo.'}}
        if($nameIndex -lt 0 -or $nameIndex -ge $names.Count){throw 'Referencia a nome invalida.'}
        if($flags -band 4) {
            $publicCount++
            $name=$names[$nameIndex]
            if([string]::IsNullOrWhiteSpace($name) -or $name -eq 'None' -or $name -match '[\x00-\x1f\x7f]') {
                throw "Export publico $($i+1) com nome invalido. Publicacao bloqueada (UnLinker NAME_None)."
            }
        }
    }
    Write-Host "Validacao: $exportCount exports, $publicCount publicos, nenhum nome publico invalido."
} finally {$reader.Dispose(); $stream.Dispose()}
