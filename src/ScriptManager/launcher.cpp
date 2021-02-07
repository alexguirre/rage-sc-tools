#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>

int main()
{
    auto l = LoadLibraryA("script-manager.dll");
    auto getUIThreadHandle = (HANDLE(*)())GetProcAddress(l, "GetUIThreadHandle");
    auto uiThread = getUIThreadHandle();
    printf("Library loaded: %p (UI thread: %p)\n", l, uiThread);
    WaitForSingleObject(uiThread, INFINITE);
}