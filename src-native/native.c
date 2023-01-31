#include <stdarg.h>
#include <stdio.h>

#define EXPORT_ME __attribute__((visibility("default")))

// The delegate that the managed runtime is expected to use.
typedef void (*managed_logger_callback)(const void* privdata, const int level, const char *formatted);

managed_logger_callback callback = NULL;
EXPORT_ME void register_managed_logger_callback(managed_logger_callback newCallback) {
    callback = newCallback;
}

EXPORT_ME void __attribute__ ((format(printf, 3, 4))) openconnect_logger_callback(void* privdata, int level, const char* format, ...) {
    va_list args;

    if (callback != NULL) {
        int formattedLength;
        va_start(args, format);
        formattedLength = vsnprintf(NULL, 0, format, args);
        va_end(args);

        if (formattedLength > 0) {
            char formatted[formattedLength + 1];
            va_start(args, format);
            vsnprintf(formatted, sizeof(formatted), format, args);
            va_end(args);

            callback(privdata, level, formatted);
        }
    }
}

