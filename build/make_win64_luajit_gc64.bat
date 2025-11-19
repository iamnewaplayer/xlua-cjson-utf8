@echo off

call "C:\Program Files\Microsoft Visual Studio\17\Enterprise\VC\Auxiliary\Build\vcvars64.bat"

echo Swtich to x64 build env
cd %~dp0\luajit-2.1.0b3\src
call msvcbuild_mt.bat gc64 static
cd ..\..
:: 确保彻底清理之前的构建文件，包括CMakeCache.txt和CMakeFiles
rd /s /q build_lj64 2>nul
:: 等待一下确保删除操作完成
ping 127.0.0.1 -n 2 > nul
mkdir build_lj64 & pushd build_lj64
:: 第一次运行CMake
cmake -DUSING_LUAJIT=ON -DGC64=ON -G "Visual Studio 17 2022" ..
:: 如果失败，再次尝试
IF %ERRORLEVEL% NEQ 0 (
    echo CMake配置失败，再次尝试...
    cmake -DUSING_LUAJIT=ON -DGC64=ON -G "Visual Studio 17 2022" ..
)
popd
cmake --build build_lj64 --config Release
rd /s /q plugin_luajit\Plugins\x86_64
md plugin_luajit\Plugins\x86_64
copy /Y build_lj64\Release\xlua.dll plugin_luajit\Plugins\x86_64\xlua.dll
pause