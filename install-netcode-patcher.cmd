cd ../
dotnet new tool-manifest
dotnet tool install --local evaisa.netcodepatcher.cli --version 3.*
cd DeathNote
start /b del "install-netcode-patcher.cmd"
