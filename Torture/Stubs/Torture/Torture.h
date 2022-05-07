//-----------------------------------------------------------------------------
//
//    ** DO NOT EDIT THIS FILE! **
//    This file was generated by a tool
//    re-running the tool will overwrite this file.
//
//-----------------------------------------------------------------------------

#ifndef _TORTURE_H_
#define _TORTURE_H_

#include <nanoCLR_Interop.h>
#include <nanoCLR_Runtime.h>
#include <nanoPackStruct.h>

typedef enum __nfpack Resources_StringResources
{
    Resources_StringResources_Cert = -5048,
} Resources_StringResources;

struct Library_Torture_Common_NfcDataMessage
{
    static const int FIELD__<DateTime>k__BackingField = 1;
    static const int FIELD__<NfcData>k__BackingField = 2;

    //--//

};

struct Library_Torture_Torture_Infrastructure_CpuStatsProvider
{
    static const int FIELD___statsBuffer = 1;

    NANOCLR_NATIVE_DECLARE(GetCpuUsageInternal___STATIC__U2__SZARRAY_I1);

    //--//

};

struct Library_Torture_Torture_Infrastructure_HttpPublisher
{
    static const int FIELD___serverThread = 1;

    //--//

};

struct Library_Torture_Torture_Infrastructure_MqttPublisher
{
    static const int FIELD___isReconnecting = 1;
    static const int FIELD___publishThread = 2;
    static const int FIELD___mqttClient = 3;
    static const int FIELD___connectionThread = 4;
    static const int FIELD___currentMessage = 5;

    //--//

};

struct Library_Torture_Torture_Infrastructure_NfcController
{
    static const int FIELD_STATIC__<CurrentMessage>k__BackingField = 0;
    static const int FIELD_STATIC__<Error>k__BackingField = 1;
    static const int FIELD_STATIC__<DataCounter>k__BackingField = 2;
    static const int FIELD_STATIC__<MqttConnection>k__BackingField = 3;
    static const int FIELD_STATIC__<NfcMissCounter>k__BackingField = 4;

    static const int FIELD___statsProvider = 1;

    //--//

};

struct Library_Torture_Torture_Infrastructure_NfcDataProvider
{
    static const int FIELD___messageQueue = 1;
    static const int FIELD___deviceThread = 2;
    static const int FIELD___device = 3;
    static const int FIELD___connectionThread = 4;
    static const int FIELD___connected = 5;

    //--//

};

struct Library_Torture_Torture_Resources
{
    static const int FIELD_STATIC__manager = 5;

    //--//

};

extern const CLR_RT_NativeAssemblyData g_CLR_AssemblyNative_Torture;

#endif  //_TORTURE_H_
