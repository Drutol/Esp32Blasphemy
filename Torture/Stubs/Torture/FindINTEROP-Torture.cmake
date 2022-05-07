#
# Copyright (c) .NET Foundation and Contributors
# See LICENSE file in the project root for full license information.
#

########################################################################################
# make sure that a valid path is set bellow                                            #
# this is an Interop module so this file should be placed in the CMakes module folder  #
# usually CMake\Modules                                                                #
########################################################################################

# native code directory
set(BASE_PATH_FOR_THIS_MODULE ${PROJECT_SOURCE_DIR}/InteropAssemblies/Torture)


# set include directories
list(APPEND Torture_INCLUDE_DIRS ${PROJECT_SOURCE_DIR}/src/CLR/Core)
list(APPEND Torture_INCLUDE_DIRS ${PROJECT_SOURCE_DIR}/src/CLR/Include)
list(APPEND Torture_INCLUDE_DIRS ${PROJECT_SOURCE_DIR}/src/HAL/Include)
list(APPEND Torture_INCLUDE_DIRS ${PROJECT_SOURCE_DIR}/src/PAL/Include)
list(APPEND Torture_INCLUDE_DIRS ${BASE_PATH_FOR_THIS_MODULE})


# source files
set(Torture_SRCS

    Torture.cpp


    Torture_Torture_Infrastructure_CpuStatsProvider_mshl.cpp
    Torture_Torture_Infrastructure_CpuStatsProvider.cpp

)

foreach(SRC_FILE ${Torture_SRCS})
    set(Torture_SRC_FILE SRC_FILE-NOTFOUND)
    find_file(Torture_SRC_FILE ${SRC_FILE}
        PATHS
	        ${BASE_PATH_FOR_THIS_MODULE}
	        ${TARGET_BASE_LOCATION}
            ${PROJECT_SOURCE_DIR}/src/Torture

	    CMAKE_FIND_ROOT_PATH_BOTH
    )
    # message("${SRC_FILE} >> ${Torture_SRC_FILE}") # debug helper
    list(APPEND Torture_SOURCES ${Torture_SRC_FILE})
endforeach()

include(FindPackageHandleStandardArgs)

FIND_PACKAGE_HANDLE_STANDARD_ARGS(Torture DEFAULT_MSG Torture_INCLUDE_DIRS Torture_SOURCES)
