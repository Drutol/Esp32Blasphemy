//-----------------------------------------------------------------------------
//
//                   ** WARNING! ** 
//    This file was generated automatically by a tool.
//    Re-running the tool will overwrite this file.
//    You should copy this file to a custom location
//    before adding any customization in the copy to
//    prevent loss of your changes when the tool is
//    re-run.
//
//-----------------------------------------------------------------------------

// Adapted from: https://github.com/espressif/esp-idf/tree/master/examples/system/freertos/real_time_stats

#include "Torture.h"
#include "Torture_Torture_Infrastructure_CpuStatsProvider.h"

#include <stdio.h>
#include <stdlib.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/semphr.h"
#include "esp_err.h"

using namespace Torture::Torture;

#define NUM_OF_SPIN_TASKS   6
#define SPIN_ITER           500000  //Actual CPU cycles used will depend on compiler optimization
#define SPIN_TASK_PRIO      2
#define STATS_TASK_PRIO     3
#define STATS_TICKS         pdMS_TO_TICKS(1000)
#define ARRAY_SIZE_OFFSET   5   //Increase this if print_real_time_stats returns ESP_ERR_INVALID_SIZE
#define RETURN_BUFFER_SIZE  1024 

static bool initialized;
static char task_names[NUM_OF_SPIN_TASKS][configMAX_TASK_NAME_LEN];
static SemaphoreHandle_t sync_spin_task;
static SemaphoreHandle_t sync_stats_task;

static void spin_task(void *arg);
static esp_err_t print_real_time_stats(TickType_t xTicksToWait, char* outputBuffer, uint16_t& outputBufferIndex);

uint16_t CpuStatsProvider::GetCpuUsageInternal( CLR_RT_TypedArray_INT8 param0, HRESULT &hr )
{

    (void)param0;
    (void)hr;
    uint16_t retValue = 0;

    ////////////////////////////////
    // implementation starts here //

    esp_err_t error;

    if(initialized == false)
    {
        CLR_EE_DBG_EVENT_BROADCAST( CLR_DBG_Commands_c_Monitor_Message, 21, "Creating spin tasks.\n", WP_Flags_c_NonCritical | WP_Flags_c_NoCaching );

        //Create semaphores to synchronize
        sync_spin_task = xSemaphoreCreateCounting(NUM_OF_SPIN_TASKS, 0);
        sync_stats_task = xSemaphoreCreateBinary();

        //Create spin tasks
        for (int i = 0; i < NUM_OF_SPIN_TASKS; i++) {
            snprintf(task_names[i], configMAX_TASK_NAME_LEN, "spin%d", i);
            xTaskCreatePinnedToCore(spin_task, task_names[i], RETURN_BUFFER_SIZE, NULL, SPIN_TASK_PRIO, NULL, tskNO_AFFINITY);
        }

        //Start all the spin tasks
        for (int i = 0; i < NUM_OF_SPIN_TASKS; i++) {
            xSemaphoreGive(sync_spin_task);
        }


        initialized = true;
    }

    error = print_real_time_stats(STATS_TICKS, (char*)param0.GetBuffer(), retValue);

    // implementation ends here   //
    ////////////////////////////////

    if(error != ESP_OK)
        return (uint16_t)error;

    return retValue;
}

//sources/esp-idf/components/freertos/include/freertos/FreeRTOS.h
//#define configGENERATE_RUN_TIME_STATS           1
//#define configUSE_TRACE_FACILITY                1

/**
 * @brief   Function to print the CPU usage of tasks over a given duration.
 *
 * This function will measure and print the CPU usage of tasks over a specified
 * number of ticks (i.e. real time stats). This is implemented by simply calling
 * uxTaskGetSystemState() twice separated by a delay, then calculating the
 * differences of task run times before and after the delay.
 *
 * @note    If any tasks are added or removed during the delay, the stats of
 *          those tasks will not be printed.
 * @note    This function should be called from a high priority task to minimize
 *          inaccuracies with delays.
 * @note    When running in dual core mode, each core will correspond to 50% of
 *          the run time.
 *
 * @param   xTicksToWait    Period of stats measurement
 *
 * @return
 *  - ESP_OK                Success
 *  - ESP_ERR_NO_MEM        Insufficient memory to allocated internal arrays
 *  - ESP_ERR_INVALID_SIZE  Insufficient array size for uxTaskGetSystemState. Trying increasing ARRAY_SIZE_OFFSET
 *  - ESP_ERR_INVALID_STATE Delay duration too short
 */
static esp_err_t print_real_time_stats(TickType_t xTicksToWait, char* outputBuffer, uint16_t& outputBufferIndex)
{
    TaskStatus_t *start_array = NULL, *end_array = NULL;
    UBaseType_t start_array_size, end_array_size;
    uint32_t start_run_time, end_run_time;
    esp_err_t ret;
    
    outputBufferIndex += snprintf(outputBuffer + outputBufferIndex, RETURN_BUFFER_SIZE - outputBufferIndex, "Hottest CPU stats on board!!!\n");

    //Allocate array to store current task states
    start_array_size = uxTaskGetNumberOfTasks() + ARRAY_SIZE_OFFSET;
    start_array = (TaskStatus_t*)platform_malloc(sizeof(TaskStatus_t) * start_array_size);
    if (start_array == NULL) {
        ret = ESP_ERR_NO_MEM;
        CLR_EE_DBG_EVENT_BROADCAST( CLR_DBG_Commands_c_Monitor_Message, 15, "ESP_ERR_NO_MEM\n", WP_Flags_c_NonCritical | WP_Flags_c_NoCaching );
        platform_free(start_array);
        platform_free(end_array);
        return ret;
    }
    //Get current task states
    start_array_size = uxTaskGetSystemState(start_array, start_array_size, &start_run_time);
    if (start_array_size == 0) {
        ret = ESP_ERR_INVALID_SIZE;
        CLR_EE_DBG_EVENT_BROADCAST( CLR_DBG_Commands_c_Monitor_Message, 21, "ESP_ERR_INVALID_SIZE\n", WP_Flags_c_NonCritical | WP_Flags_c_NoCaching );
        platform_free(start_array);
        platform_free(end_array);
        return ret;
    }

    outputBufferIndex += snprintf(outputBuffer + outputBufferIndex, RETURN_BUFFER_SIZE - outputBufferIndex, "start_run_time: %d \n", start_run_time);
    vTaskDelay(xTicksToWait);

    //Allocate array to store tasks states post delay
    end_array_size = uxTaskGetNumberOfTasks() + ARRAY_SIZE_OFFSET;
    end_array = (TaskStatus_t*)platform_malloc(sizeof(TaskStatus_t) * end_array_size);
    if (end_array == NULL) {
        ret = ESP_ERR_NO_MEM;
        CLR_EE_DBG_EVENT_BROADCAST( CLR_DBG_Commands_c_Monitor_Message, 15, "ESP_ERR_NO_MEM\n", WP_Flags_c_NonCritical | WP_Flags_c_NoCaching );
        platform_free(start_array);
        platform_free(end_array);
        return ret;
    }
    //Get post delay task states
    end_array_size = uxTaskGetSystemState(end_array, end_array_size, &end_run_time);
    if (end_array_size == 0) {
        ret = ESP_ERR_INVALID_SIZE;
        CLR_EE_DBG_EVENT_BROADCAST( CLR_DBG_Commands_c_Monitor_Message, 21, "ESP_ERR_INVALID_SIZE\n", WP_Flags_c_NonCritical | WP_Flags_c_NoCaching );
        platform_free(start_array);
        platform_free(end_array);
        return ret;
    }

    //Calculate total_elapsed_time in units of run time stats clock period.
    outputBufferIndex += snprintf(outputBuffer + outputBufferIndex, RETURN_BUFFER_SIZE - outputBufferIndex, "end_run_time: %d \n", end_run_time);
    uint32_t total_elapsed_time = (end_run_time - start_run_time);
    if (total_elapsed_time == 0) {
        ret = ESP_ERR_INVALID_STATE;
        CLR_EE_DBG_EVENT_BROADCAST( CLR_DBG_Commands_c_Monitor_Message, 22, "ESP_ERR_INVALID_STATE\n", WP_Flags_c_NonCritical | WP_Flags_c_NoCaching );
        platform_free(start_array);
        platform_free(end_array);
        return ret;
    }

    outputBufferIndex += snprintf(outputBuffer + outputBufferIndex, RETURN_BUFFER_SIZE - outputBufferIndex, "| Task | Run Time | Percentage\n");
    //Match each task in start_array to those in the end_array
    for (int i = 0; i < start_array_size; i++) {
        int k = -1;
        for (int j = 0; j < end_array_size; j++) {
            if (start_array[i].xHandle == end_array[j].xHandle) {
                k = j;
                //Mark that task have been matched by overwriting their handles
                start_array[i].xHandle = NULL;
                end_array[j].xHandle = NULL;
                break;
            }
        }
        //Check if matching task found
        if (k >= 0) {
            uint32_t task_elapsed_time = end_array[k].ulRunTimeCounter - start_array[i].ulRunTimeCounter;
            uint32_t percentage_time = (task_elapsed_time * 100UL) / (total_elapsed_time * portNUM_PROCESSORS);
            outputBufferIndex += snprintf(outputBuffer + outputBufferIndex, RETURN_BUFFER_SIZE - outputBufferIndex, "| %s | %d | %d%%\n", start_array[i].pcTaskName, task_elapsed_time, percentage_time);
        }
    }

    //Print unmatched tasks
    for (int i = 0; i < start_array_size; i++) {
        if (start_array[i].xHandle != NULL) {
            outputBufferIndex += snprintf(outputBuffer + outputBufferIndex, RETURN_BUFFER_SIZE - outputBufferIndex, "| %s | Deleted\n", start_array[i].pcTaskName);
        }
    }
    for (int i = 0; i < end_array_size; i++) {
        if (end_array[i].xHandle != NULL) {
            outputBufferIndex += snprintf(outputBuffer + outputBufferIndex, RETURN_BUFFER_SIZE - outputBufferIndex, "| %s | Created\n", end_array[i].pcTaskName);
        }
    }

    ret = ESP_OK;
    return ret;
}

static void spin_task(void *arg)
{
    xSemaphoreTake(sync_spin_task, portMAX_DELAY);
    while (1) {
        //Consume CPU cycles
        for (int i = 0; i < SPIN_ITER; i++) {
            __asm__ __volatile__("NOP");
        }
        vTaskDelay(pdMS_TO_TICKS(100));
    }
}