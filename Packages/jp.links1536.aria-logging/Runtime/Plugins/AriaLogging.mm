#import <os/log.h>

extern "C"
{
	void _NativeOSLog(int type, const char* category, const char* message) {
		// アプリのBundle IdentifierをSubsystemに使用
		os_log_t log = os_log_create("com.yourcompany.yourapp", category);
		
		os_log_type_t logType;
		switch (type)
		{
			case 1: logType = OS_LOG_TYPE_INFO; break;
			case 2: logType = OS_LOG_TYPE_DEBUG; break;
			case 3: logType = OS_LOG_TYPE_ERROR; break;
			case 4: logType = OS_LOG_TYPE_FAULT; break;
			default: logType = OS_LOG_TYPE_DEFAULT; break;
		}

		os_log_with_type(log, logType, "%{public}s", message);
	}
}