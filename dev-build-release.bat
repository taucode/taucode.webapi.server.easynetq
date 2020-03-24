dotnet restore

dotnet build --configuration Debug
dotnet build --configuration Release

dotnet test -c Debug .\tests\TauCode.WebApi.Server.EasyNetQ.Tests\TauCode.WebApi.Server.EasyNetQ.Tests.csproj
dotnet test -c Release .\tests\TauCode.WebApi.Server.EasyNetQ.Tests\TauCode.WebApi.Server.EasyNetQ.Tests.csproj

nuget pack nuget\TauCode.WebApi.Server.EasyNetQ.nuspec                                                                     