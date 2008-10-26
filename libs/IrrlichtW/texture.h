#include "main.h"

extern "C"
{
    EXPORT ECOLOR_FORMAT Texture_GetColorFormat(IntPtr texture);
    EXPORT E_DRIVER_TYPE Texture_GetDriverType(IntPtr texture);
    EXPORT void Texture_GetOriginalSize(IntPtr texture, M_DIM2DS toR);
    EXPORT s32 Texture_GetPitch(IntPtr texture);
	/*EXPORT void Texture_GetTransform(IntPtr texture, M_MAT4 TxT);
	EXPORT void Texture_SetTransform(IntPtr texture, M_MAT4 TxT);*/
	EXPORT void Texture_RegenerateMipMapLevels(IntPtr texture);
	EXPORT IntPtr Texture_Lock(IntPtr texture);
	EXPORT void Texture_UnLock(IntPtr texture);
	EXPORT IntPtr Texture_GetName(IntPtr texture); 

	EXPORT void LockResult_GetPixel(IntPtr lock, IntPtr texture, int x, int y, M_SCOLOR color);
	EXPORT void LockResult_SetPixel(IntPtr lock, IntPtr texture, int x, int y, M_SCOLOR color);
}
