################################################################################
#
# OpenHD
#
################################################################################
OPENHD_SITE = https://github.com/OpenHD/OpenHD
OPENHD_SITE_METHOD = git
OPENHD_GIT_SUBMODULES = YES
OPENHD_VERSION = v2.5.3
OPENHD_SUBDIR = OpenHD

OPENHD_INSTALL_STAGING = NO
OPENHD_INSTALL_TARGET = YES

OPENHD_DEPENDENCIES = libsodium gstreamer1 gst1-plugins-base gst1-plugins-bad libpcap libusb libv4l host-pkgconf
$(eval $(cmake-package))