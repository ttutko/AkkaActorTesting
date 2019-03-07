import logging
import os
import time
import sys

iterations = 0
max_iters = os.environ.get('SPT_MaxIters', None)
exit_code = os.environ.get('SPT_ExitCode', '0')

while(True):
    print("Running...." + os.environ.get('SPT_EnvVar', ""))
    sys.stdout.flush()
    iterations = iterations + 1
    if(max_iters and int(max_iters) <= iterations):
            break
    time.sleep(1)

exit(int(exit_code))