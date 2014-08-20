#!/bin/sh

npm install
bower install
bake packages:install | iconv -s -f cp866 -t cp1251
bake generate:assembly:info | iconv -s -f cp866 -t cp1251
bake fix:packages | iconv -s -f cp866 -t cp1251
