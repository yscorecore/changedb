#/bin/bash
PUBLISH_FOLDER=$1
NUGET_APIKEY=$2
for file in `ls $PUBLISH_FOLDER/*.nupkg`
do
    dotnet nuget push $file -k $NUGET_APIKEY -s  https://api.nuget.org/v3/index.json --skip-duplicate
done