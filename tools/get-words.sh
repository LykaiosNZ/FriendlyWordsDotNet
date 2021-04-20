#!/usr/bin/env bash
version="1.1.10"
currentDir="${0%/*}"

echo "Installing friendly-words@$version"
npm install -d --silent friendly-words@$version

echo
echo "Copying words files"
cp -R -v ./node_modules/friendly-words/words $currentDir/../src

echo
echo "Cleaning up"
npm uninstall --silent friendly-words
rm -R ./package-lock.json