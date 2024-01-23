################################################################################
#
# OpenHD
#
################################################################################
OPENHD_VERSION = 1.0
OPENHD_SOURCE = openhd-$(OPENHD_VERSION).tar.gz
OPENHD_SITE = http://www.foosoftware.org/download
OPENHD_INSTALL_STAGING = YES
OPENHD_INSTALL_TARGET = NO
OPENHD_CONF_OPTS = -DBUILD_DEMOS=ON
OPENHD_DEPENDENCIES = libglib2 host-pkgconf
$(eval $(cmake-package))