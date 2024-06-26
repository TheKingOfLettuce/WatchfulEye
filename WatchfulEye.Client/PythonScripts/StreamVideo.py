import socket
import time
import picamera
import sys

def main():
    args = sys.argv[1:]
    print("Running StreamVideo with args", args)
    StreamCamera(int(args[0]), int(args[1]), int(args[2]), args[3], int(args[4]), int(args[5]))
    sys.exit(1)

def StreamCamera(width, height, framerate, server, port, recordTime):
    client_socket = socket.socket()
    client_socket.connect((server, port))
    # Make a file-like object out of the connection
    connection = client_socket.makefile('wb')
    camera = picamera.PiCamera()
    try:
        camera.resolution = (width, height)
        camera.framerate = framerate
        camera.start_preview()
        #allow camera to "warm up"
        time.sleep(2)
        camera.start_recording(connection, format='mjpeg')
        camera.wait_recording(recordTime)
        
    finally:
        camera.stop_recording()
        camera.stop_preview()
        connection.close()
        client_socket.close()


if __name__ == "__main__":
    main()