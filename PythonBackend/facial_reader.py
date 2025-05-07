from deepface import DeepFace
import cv2
from PIL import Image
import numpy as np
import cv2

def read_image(image):
    img = read_image_as_cv2(image)
    result = DeepFace.analyze(img, actions=['emotion'], enforce_detection=False)

    print(result[0]['emotion'])

    max = 0
    category = ""
    for k, v in result[0]['emotion'].items():
        if v > max:
            max = v
            category = k

    return (category, max)

def read_image_as_cv2(file):
    image = Image.open(file.stream).convert('RGB')
    np_img = np.array(image)
    cv2_img = cv2.cvtColor(np_img, cv2.COLOR_RGB2BGR)
    return cv2_img


if __name__ == "__main__":
    emotions = read_image("./ck+/happy/S010_006_00000013.png")
    print(emotions)
