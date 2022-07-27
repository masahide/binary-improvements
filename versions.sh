#!/bin/bash
VERSIONS=""
for F in `find bin -name ModInfo.xml | sort`
do
	xmlstarlet sel -T -t -v /xml/ModInfo/Name/@value -o ": " -v /xml/ModInfo/Version/@value -n $F
	VERSIONS="${VERSIONS}`xmlstarlet sel -T -t -v /xml/ModInfo/Version/@value $F`_"
done
VERSIONS=${VERSIONS:0:-1}
echo "Combined: $VERSIONS"

