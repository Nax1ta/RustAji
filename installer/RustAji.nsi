Unicode true
ManifestDPIAware true

!define APP_VERSION "1.1.0"
!define PROJECT_ROOT ".."
!include "MUI2.nsh"

Name "RustAji 腐味"
OutFile "${PROJECT_ROOT}\..\..\outputs\RustAji-${APP_VERSION}-setup-windows-x64.exe"
InstallDir "$LOCALAPPDATA\Programs\RustAji"
InstallDirRegKey HKCU "Software\RustAji" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma
Icon "${PROJECT_ROOT}\TutorialLibrary\Assets\avatar.ico"
UninstallIcon "${PROJECT_ROOT}\TutorialLibrary\Assets\avatar.ico"
VIProductVersion "1.1.0.0"
VIAddVersionKey "ProductName" "RustAji 腐味"
VIAddVersionKey "FileDescription" "RustAji 安装程序"
VIAddVersionKey "FileVersion" "${APP_VERSION}"
VIAddVersionKey "LegalCopyright" "RustAji contributors"

Var DataDir
Var FreshInstall

!define MUI_ICON "${PROJECT_ROOT}\TutorialLibrary\Assets\avatar.ico"
!define MUI_UNICON "${PROJECT_ROOT}\TutorialLibrary\Assets\avatar.ico"
!define MUI_BGCOLOR "F1F4F0"
!define MUI_TEXTCOLOR "303630"
!define MUI_ABORTWARNING

!define MUI_PAGE_HEADER_TEXT "程序安装位置"
!define MUI_PAGE_HEADER_SUBTEXT "可安装到 C 盘以外的目录。"
!insertmacro MUI_PAGE_DIRECTORY

!define MUI_DIRECTORYPAGE_VARIABLE $DataDir
!define MUI_PAGE_CUSTOMFUNCTION_PRE DataPagePre
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE DataPageLeave
!define MUI_PAGE_HEADER_TEXT "资料保存位置"
!define MUI_PAGE_HEADER_SUBTEXT "收藏记录和图片与程序文件分开保存。"
!define MUI_DIRECTORYPAGE_TEXT_TOP "选择资料目录。卸载程序不会删除这个目录。"
!insertmacro MUI_PAGE_DIRECTORY

!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\RustAji.exe"
!define MUI_FINISHPAGE_RUN_TEXT "启动 RustAji"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "SimpChinese"

Function .onInit
  StrCpy $DataDir "$LOCALAPPDATA\RustAji"
  StrCpy $FreshInstall "1"
  IfFileExists "$APPDATA\RustAji\location.json" ExistingLibrary
  IfFileExists "$LOCALAPPDATA\RustAji\library.json" ExistingLibrary
  IfFileExists "$APPDATA\腐味\location.json" ExistingLibrary
  IfFileExists "$LOCALAPPDATA\腐味\library.json" ExistingLibrary
  Goto InitDone
ExistingLibrary:
  StrCpy $FreshInstall "0"
InitDone:
FunctionEnd

Function DataPagePre
  StrCmp $FreshInstall "0" 0 +2
    Abort
FunctionEnd

Function DataPageLeave
  StrCmp $DataDir "" 0 +3
    MessageBox MB_ICONSTOP|MB_OK "请选择资料保存目录。"
    Abort
  StrCmp $DataDir $INSTDIR 0 +3
    MessageBox MB_ICONSTOP|MB_OK "资料目录不能与程序安装目录相同。"
    Abort
  IfFileExists "$DataDir\library.json" DataPageValid
  IfFileExists "$DataDir\*.*" 0 DataPageValid
    MessageBox MB_ICONSTOP|MB_OK "所选目录不是空目录，也不是已有的 RustAji 资料库。请选择空目录。"
    Abort
  DataPageValid:
FunctionEnd

Section "安装 RustAji"
  SetOutPath "$INSTDIR"
  File "${PROJECT_ROOT}\publish\RustAji.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\RustAji"
  CreateShortcut "$SMPROGRAMS\RustAji\RustAji.lnk" "$INSTDIR\RustAji.exe"
  CreateShortcut "$SMPROGRAMS\RustAji\卸载 RustAji.lnk" "$INSTDIR\Uninstall.exe"

  WriteRegStr HKCU "Software\RustAji" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RustAji" "DisplayName" "RustAji 腐味"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RustAji" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RustAji" "DisplayIcon" "$INSTDIR\RustAji.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RustAji" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RustAji" "Publisher" "RustAji"
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RustAji" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RustAji" "NoRepair" 1

  StrCmp $FreshInstall "1" 0 DoneDataRequest
    CreateDirectory "$APPDATA\RustAji"
    ClearErrors
    FileOpen $0 "$APPDATA\RustAji\install-location.txt" w
    IfErrors DataRequestFailed
    FileWriteUTF16LE /BOM $0 "$DataDir$\r$\n"
    FileClose $0
    IfErrors DataRequestFailed
    Goto DoneDataRequest
  DataRequestFailed:
    Abort "无法保存资料目录设置。请检查目录权限后重新安装。"
  DoneDataRequest:
SectionEnd

Section "Uninstall"
  Delete "$SMPROGRAMS\RustAji\RustAji.lnk"
  Delete "$SMPROGRAMS\RustAji\卸载 RustAji.lnk"
  RMDir "$SMPROGRAMS\RustAji"
  Delete "$INSTDIR\RustAji.exe"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\RustAji"
  DeleteRegKey HKCU "Software\RustAji"
  ; Personal data under LocalAppData and AppData is deliberately retained.
SectionEnd
