#!/bin/bash

url="http://localhost:5046/start"
requests=${1:-50}

# Invoke bombardier with additional options
bombardier -m POST -H 'Content-Type: application/json' -b '{"name": "Task"}' -n $requests $url