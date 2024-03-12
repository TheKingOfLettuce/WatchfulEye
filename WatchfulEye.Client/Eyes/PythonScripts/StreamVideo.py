import socket
import time
import picamera
import sys

def main():
    args = sys.argv[1:]
    StreamCamera(args[0], args[1], args[2], args[3], args[4], args[5])

def StreamCamera(width, height, framerate, server, port, time):
    client_socket = socket.socket()
    client_socket.connect((server, port))
    # Make a file-like object out of the connection
    connection = client_socket.makefile('wb')
    try:
        camera = picamera.PiCamera()
        camera.resolution = (640, 480)
        camera.framerate = 24
        # Start a preview and let the camera warm up for 2 seconds
        camera.start_preview()
        time.sleep(2)
        # Start recording, sending the output to the connection for 60
        # seconds, then stop
        camera.start_recording(connection, format='h264')
        camera.wait_recording(60)
        camera.stop_recording()
        camera.stop_preview()
    finally:
        connection.close()
        client_socket.close()


if __name__ == "__main__":
    main()