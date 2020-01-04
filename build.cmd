@ECHO OFF

docker build ^
 -f build.Dockerfile ^
 --tag proxykit-routinghandler-build .

if errorlevel 1 (
  echo Docker build failed: Exit code is %errorlevel%
  exit /b %errorlevel%
)

docker run --rm -it --name proxykit-routinghandler-build ^
 -v %cd%:/repo ^
 -w /repo ^
 -e FEEDZ_PROXYKIT_API_KEY=%FEEDZ_PROXYKIT_API_KEY% ^
 proxykit-routinghandler-build ^
 dotnet run -p build/build.csproj -c Release -- %*

if errorlevel 1 (
  echo Docker build failed: Exit code is %errorlevel%
  exit /b %errorlevel%
)