#!/bin/sh

npm install
bower install
bake packages:install
bake generate:assembly:info
