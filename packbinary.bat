copy Moto_Logo\bin\Release\Moto_Boot_Logo_Maker.exe MotoBootLogoMaker.exe
copy Moto_Logo\bin\Release\Moto_Boot_Logo_Maker.exe.config MotoBootLogoMaker.exe.config
7z a -tZip -y MotoBootLogoMaker.zip MotoBootLogoMaker.exe MotoBootLogoMaker.exe.config
del MotoBootLogoMaker.exe
del MotoBootLogoMaker.exe.config
