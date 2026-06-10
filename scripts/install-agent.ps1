# install-agent.ps1
# Run this from anywhere to install the agent package + pywin32 using the correct Python.
$py = "C:\Users\chris\AppData\Local\Python\pythoncore-3.14-64\python.exe"
$project = "C:\Users\chris\my-project"
Write-Host "Installing agent extras using $py ..."
& $py -m pip install -e "$project[agent]"
Write-Host "Done. You can now run the server with .\scripts\run-server.ps1"
