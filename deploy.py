#!/usr/bin/env python3
"""
极地生存法则 — 一键部署脚本
用法: python deploy.py

服务器: root@101.34.30.65
路径:   /opt/drscfz/
PM2:    drscfz-server
"""

import subprocess
import sys
import os

SERVER = "root@101.34.30.65"
REMOTE_PATH = "/opt/drscfz"
LOCAL_SERVER = os.path.join(os.path.dirname(__file__), "Server")

# 需要同步的目录（相对于 Server/）
SYNC_DIRS = ["src", "config"]

def run(cmd, check=True):
    print(f"$ {cmd}")
    result = subprocess.run(cmd, shell=True, text=True, capture_output=False)
    if check and result.returncode != 0:
        print(f"ERROR: command failed with code {result.returncode}")
        sys.exit(1)
    return result.returncode

def main():
    print("=" * 50)
    print("极地生存法则 服务器部署")
    print("=" * 50)

    # 1. 同步文件
    for d in SYNC_DIRS:
        local = os.path.join(LOCAL_SERVER, d).replace("\\", "/")
        remote = f"{SERVER}:{REMOTE_PATH}/{d}/"
        print(f"\n[同步] {d}/ → {remote}")
        # 使用 scp -r 或 rsync (优先 rsync)
        rsync_cmd = f'rsync -avz --delete "{local}/" "{remote}"'
        if run(rsync_cmd, check=False) != 0:
            # fallback to scp
            print("rsync 失败，尝试 scp...")
            run(f'scp -r "{local}" {SERVER}:{REMOTE_PATH}/')

    # 2. 重启 PM2
    print("\n[重启] PM2 进程 drscfz-server")
    run(f'ssh {SERVER} "pm2 restart drscfz-server && pm2 save"')

    # 3. 健康检查
    print("\n[检查] 服务健康状态...")
    import time
    time.sleep(3)
    run(f'curl -s http://101.34.30.65:8081/health | python3 -m json.tool', check=False)

    print("\n✅ 部署完成！")

if __name__ == "__main__":
    main()
