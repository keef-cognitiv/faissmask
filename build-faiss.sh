#!/bin/bash

set -e

FAISS_VERSION=""
FAISS_OS=""
FAISS_ARCH=""

print_usage() {
	echo "Usage $0 -v <VERSION> -o <linux|windows|macos> -a <arm64|amd64>"
}

while true; do
	case "$1" in
		-v | --version ) FAISS_VERSION="$2"; shift 2 ;;
		-o | --os ) FAISS_OS="$2"; shift 2 ;;
		-a | --arch ) FAISS_ARCH="$2"; shift 2 ;;
		* ) break ;;
	esac
done

if [[ -z "$FAISS_VERSION" ]]; then
	print_usage
	exit -1
fi

FAISS_DOTNET_OS=""

case "${FAISS_OS}" in
	linux ) 
		FAISS_DOTNET_OS="linux"
		;;
	macos )
		FAISS_DOTNET_OS="osx"
		;;
	windows )
		FAISS_DOTNET_OS="win"
		;;
	* )
		print_usage
		exit -1
		;;
esac

FAISS_DOCKER_ARCH=""
FAISS_DOTNET_ARCH=""

case "${FAISS_ARCH}" in
	arm64 )
		FAISS_DOTNET_ARCH="arm64"
		;;
	amd64 )
		FAISS_DOTNET_ARCH="x64"
		;;
	* )
		print_usage
		exit -1
esac

if [[ "${FAISS_OS}" == "linux" ]]; then
	docker compose build --progress plain --build-arg FAISS_VERSION=${FAISS_VERSION} --build-arg FAISS_DOTNET_ARCHITECTURE="${FAISS_DOTNET_ARCH}" "${FAISS_OS}-${FAISS_ARCH}"
	rm -f FaissMask/runtimes/linux-x64/native/*
	docker run --rm -v $PWD:/host faissmask-"${FAISS_OS}-${FAISS_ARCH}" bash -c "cp /src/FaissMask/runtimes/${FAISS_DOTNET_OS}-${FAISS_DOTNET_ARCH}/native/* /host/FaissMask/runtimes/${FAISS_DOTNET_OS}-${FAISS_DOTNET_ARCH}/native/"

else
	echo "Cross OS build not yet implemented"
	exit -1
fi
