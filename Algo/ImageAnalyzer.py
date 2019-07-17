import numpy as np
import cv2
from matplotlib import pyplot as plt
from matplotlib.patches import Rectangle


class ImageAnalyzer:
    def __init__(self, path, debug=False):
        self.path = path
        self.debug = debug
        self.rects = []

    def extract_text_areas(self):
        receiptOrigin = cv2.imread(self.path, cv2.IMREAD_GRAYSCALE)  # image receipt load

        xflt = np.array([[1, -1]])
        yflt = np.array([[1], [-1]])
        receipt = receiptOrigin.astype('int16')
        dx = cv2.filter2D(receipt, -1, xflt)
        dy = cv2.filter2D(receipt, -1, yflt)

        _, thDx = cv2.threshold(abs(dx), 30, 1, cv2.THRESH_BINARY)  # future idea: adaptive threshold can be considered
        _, thDy = cv2.threshold(abs(dy), 30, 1, cv2.THRESH_BINARY)  # future idea: define threshold with histogram
        thDx = np.array(thDx)
        thDy = np.array(thDy)
        textMap = np.array(thDx * thDy)

        gauss1D = cv2.getGaussianKernel(15, 5)  # gaussian filter generation
        gauss1D = np.reshape(gauss1D, (1, 15))  # transpose
        gaussFlt = np.array(gauss1D.repeat(3, 0))  # replicate 3 times to get 3x15 matrix

        textMap = np.array(cv2.filter2D(textMap, ddepth=cv2.CV_32F, kernel=gaussFlt))

        _, binTextMap = cv2.threshold(textMap, 0.1, 1, cv2.THRESH_BINARY)
        binTextMap = np.array(binTextMap)
        components = cv2.connectedComponentsWithStats(binTextMap.astype('byte'))

        rects = np.array(components[2])
        rects = rects[1:]  # remove the background rectangle, all image rect ,located at the first row
        rects = rects[rects[:, 3] > 4]
        scores = rects[:, 4] / (rects[:, 2] * rects[:, 3])  #

        if self.debug:
            plt.imshow(receiptOrigin, cmap='gray')
            ax = plt.gca()
            for rect in rects:
                ax.add_patch(Rectangle((rect[0], rect[1] - 1), rect[2], rect[3] + 1, linewidth=1, edgecolor='r',
                                       facecolor='none'))
            plt.xticks([]), plt.yticks([])  # to hide tick values on x and y axis
            plt.show()

        self.rects = rects
