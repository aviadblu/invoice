# import PyPDF2
import sys
import json
import argparse
from pathlib import Path

import imghdr
import ImageAnalyzer

debugMode = False
######################################
# arguments validation
######################################
parser = argparse.ArgumentParser(description='Process invoice')
parser.add_argument('--image', help='image path')

args = parser.parse_args()

# check if file path supplied
if args.image is None:
    print(json.dumps({
        "status": "error",
        "message": "no image path"}))
    exit(0)

# check if file exist
image_file = Path(args.image)
if image_file.is_file() is False:
    print(json.dumps({
        "status": "error",
        "message": "path file not found"}))
    exit(0)

# check if file supported
supportedFiles = ["png", "jpg", "jpeg", "bmp"]
fileType = imghdr.what(args.image);
if fileType not in supportedFiles:
    print(json.dumps({
        "status": "error",
        "message": "File type not supported, expected [" + ', '.join(
        [str(x) for x in supportedFiles]) + "] and got `" + fileType + "`"}))
    exit(0)
########################################


try:
    imageAnalyzer = ImageAnalyzer.ImageAnalyzer(args.image)
    imageAnalyzer.extract_text_areas()
    result = {
        "status": "success",
        "rects": imageAnalyzer.rects.tolist()
    }
    print(json.dumps(result))
except (RuntimeError, TypeError, NameError) as err:
    print(json.dumps({"error": "unexpected error: " + str(err)}))
