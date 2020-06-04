function Process([string[]]$files, [string]$search, [string]$replace) 
{
	foreach ($file in Get-ChildItem $files) {
		(Get-Content $file) | 
		Foreach-Object {$_ -replace $search, $replace}  | 
		Out-File -Encoding UTF8 $file
	 }
}

$BUILDINFOS = "$Env:BUILD_DEFINITIONNAME/$Env:BUILD_SOURCEBRANCHNAME/$Env:BUILD_SOURCEVERSION"
$YEAR = Get-Date -UFormat "%Y"

if ($BUILDINFOS -eq "//") {
	$BUILDINFOS = "custom build"
}

Process ("libs/ProductInfo.cs") ("AssemblyInformationalVersion\(.*\)") ("AssemblyInformationalVersion(""$BUILDINFOS"")")
Process ("libs/ProductInfo.cs") ("AssemblyCopyright\(.*\)") ("AssemblyCopyright(""Copyright $([char]0x00A9) Microsoft Corporation $YEAR"")")
