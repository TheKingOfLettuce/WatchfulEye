import socket
import time
import picamera
import sys

def main():
    args = sys.argv[1:]
    print("Running TakePicture with args", args)
    StreamCamera(int(args[0]), int(args[1]), args[2], int(args[3]))
    sys.exit(1)

def StreamCamera(width, height, server, port):
    client_socket = socket.socket()
    client_socket.connect((server, port))
    # Make a file-like object out of the connection
    connection = client_socket.makefile('wb')
    camera = picamera.PiCamera()
    try:
        camera.resolution = (width, height)
        camera.start_preview()
        #allow camera to "warm up"
        time.sleep(2)
        camera.capture(connection, 'jpeg')
        
    finally:
        camera.stop_preview()
        connection.close()
        client_socket.close()


if __name__ == "__main__":
    main()