# openhd-build-ng

## How to build for OpenIPC
```sh
./build.sh BuildOpenIpc
```

# Developing plan
## Stage1 (Done)
DoD:
* creation of sysroot for raspberry
* creating cmake toolchain file for building OpenHD with compiller from debian repository

Prerequrements
```
sudo apt install -y mmdebstrap gcc-10-arm-linux-gnueabihf g++-10-arm-linux-gnueabihf
```

## Stage OpenIPC
DoD:
* OpenHD building for Sigmastar

## Stage2
DoD:
* downloading and using **latest** cmake

## Stage3
DoD:
* compiling gcc crosscompillers
* ...

## StageN
Support for all our platforms