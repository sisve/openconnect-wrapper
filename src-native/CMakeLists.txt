cmake_minimum_required(VERSION 3.5)
project(native)

if (WIN32)
    add_library(native.x64 SHARED native.c)
endif (WIN32)
