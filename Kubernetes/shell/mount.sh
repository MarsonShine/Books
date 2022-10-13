domount() {
  MNTPATH=$1

  NFS_SERVER=$(echo $2 | jq -r '.server')
  SHARE=$(echo $2 | jq -r '.share')

  mkdir -p ${NFS_SERVER}:/${SHARE} ${MNTPATH} &> /dev/null
  if [$? -ne 0]; then
    err "{ \"status\": \"Failure\", \"message\": \"Failed to mount ${NFS_SERVER}:${SHARE} at ${MNTPATH}\"}"
    exit 1
  fi
  log '{"status": "Success"}'
  exit 0
}