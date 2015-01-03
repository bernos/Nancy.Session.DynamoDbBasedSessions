Properties {
    $version = $null
    $projects = $null
    $configuration = "Release"
}

Task Default -Depends Test

Task Publish -Depends Package {
    foreach($project in $projects) {
        Get-ChildItem | Where-Object -FilterScript {
            ($_.Name.Contains("$project.$version")) -and !($_.Name.Contains(".symbols")) -and ($_.Extension -eq '.nupkg')    
        } | ForEach-Object {
            exec { nuget push $_.FullName }
        }
    }
}

Task Package -Depends Set-Versions,Test {
    foreach($project in $projects) {
        Get-ChildItem -Path "$project\*.csproj" | ForEach-Object {            
            exec { nuget pack -sym $_.FullName -Prop Configuration=$configuration }
        }        
    }
}

Task Build -Depends Clean {
    Exec { msbuild "$Solution" /t:Build /p:Configuration=$configuration } 
}

task Test -Depends Build {
	Exec { .\packages\xunit.runners.1.9.2\tools\xunit.console.clr4.exe ".\Nancy.Session.DynamoDbBasedSessions.Tests\bin\$configuration\Nancy.Session.DynamoDbBasedSessions.Tests.dll" /noshadow }
	
	
}

Task Clean {
    Exec { msbuild "$Solution" /t:Clean /p:Configuration=$configuration } 
}

Task Set-Versions {
    if ($version) {        
        Get-ChildItem -Recurse -Force | Where-Object { $_.Name -eq "AssemblyInfo.cs" } | ForEach-Object {
            (Get-Content $_.FullName) | ForEach-Object {
                ($_ -replace 'AssemblyVersion\(.*\)', ('AssemblyVersion("' + $version + '")')) -replace 'AssemblyFileVersion\(.*\)', ('AssemblyFileVersion("' + $version + '")')
            } | Set-Content $_.FullName -Encoding UTF8
        }
    } else {
        throw "Please specify a version number."
    }    
}