HOST=136.243.11.25
PORT=51010
SERVERPATH=/srv/www/illy.bz/http/fi/7dtd/

$(shell cd .. && ./versions.sh > bin/Mods/Allocs_CommonFunc/7dtd-server-fixes_version.txt)
VERSIONFILE=../bin/Mods/Allocs_CommonFunc/7dtd-server-fixes_version.txt
VERSION=$(shell cat ${VERSIONFILE} | grep "Combined" | cut -d\  -f2)
ARCHIVENAME=server_fixes_v${VERSION}.tar.gz

build: CONTENTS ${ARCHIVENAME}
	@echo Bundle built successfully

all: build upload
	@echo Bundle built and uploaded

upload: ${ARCHIVENAME}
	@echo Uploading files...
	@scp -P ${PORT} ${ARCHIVENAME} ${VERSIONFILE} ${HOST}:${SERVERPATH}
	@ssh -p ${PORT} ${HOST} "cd ${SERVERPATH}; rm -f server_fixes.tar.gz; ln -s ${ARCHIVENAME} server_fixes.tar.gz"

${ARCHIVENAME}: CONTENTS
	@echo Creating archive...
	@tar -czf ${ARCHIVENAME} Mods

CONTENTS: SERVERFIXES

SERVERFIXES:
	@echo Copying server fixes...
	@rm Mods -Rf
	@mkdir -p Mods
	@cp ../bin/Mods/* Mods/ -R
	@mkdir -p Mods/Allocs_WebAndMapRendering/webserver
	@cp ../webserver/* Mods/Allocs_WebAndMapRendering/webserver/ -R
	@rm Mods/Allocs_WebAndMapRendering/webserver/protect -f
	@find . -name "*~" -exec rm {} \;

clean:
	@echo Cleaning intermediate stuff...
	@rm -Rf Mods server_fixes_v*.tar.gz

