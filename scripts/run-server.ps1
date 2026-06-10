# Run this to start the PR3 agent server reliably.
# It forces the correct Python 3.14 (with pywin32) and correct project directory.
$py = "C:\Users\chris\AppData\Local\Python\pythoncore-3.14-64\python.exe"
$project = "C:\Users\chris\my-project"
Push-Location $project
Write-Host "Starting server from $project using $py ..."
& $py -m agent.server
Pop-Location
