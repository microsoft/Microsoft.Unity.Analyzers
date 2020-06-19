function Process([string[]]$files, [string]$search, [string]$replace) 
{
	foreach ($file in Get-ChildItem $files) {
		(Get-Content $file) | 
		Foreach-Object {$_ -replace $search, $replace}  | 
		Out-File -Encoding UTF8 $file
	 }
}

$VERSION_SHORT = "1.7.1"
$VERSION_LONG = $VERSION_SHORT + ".0"
$VERSIONSEARCH_SHORT = "\d+\.\d+\.\d+"
$VERSIONSEARCH_LONG = $VERSIONSEARCH_SHORT + "\.\d+"

Process ("libs/ProductInfo.cs") ($VERSIONSEARCH_LONG) ($VERSION_LONG)
Process ("build/UnityVS.props") ('<VersionPrefix>' + $VERSIONSEARCH_SHORT) ('<VersionPrefix>' + $VERSION_SHORT)
