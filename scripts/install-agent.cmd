@echo off
set py=C:\Users\chris\AppData\Local\Python\pythoncore-3.14-64\python.exe
set project=C:\Users\chris\my-project
echo Installing agent extras using %py% ...
%py% -m pip install -e "%project%[agent]"
echo Done. You can now run the server with scripts\run-server.cmd
pause
