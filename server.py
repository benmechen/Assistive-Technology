import socket
import netifaces as ni
import datetime
from zeroconf import ServiceInfo, Zeroconf


HOST = ''          # Open server up on local network
PORT = 1024        # Port to listen on (non-privileged ports are > 1023)

def get_ip_address(ifname):
    ni.ifaddresses(ifname)
    # return "127.0.0.1"
    return ni.ifaddresses(ifname)[ni.AF_INET][0]['addr']


print("#####################################")
print("#### ASSISTIVE TECHNOLOGY SERVER ####")
print("#####################################\n")

serverSocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
serverSocket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
serverSocket.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
serverSocket.bind((HOST, PORT))
print(" > Server started on " + socket.gethostname() + ":" + str(PORT))

zeroconf = Zeroconf()

fqdn = socket.gethostname()
ip_addr = socket.gethostbyname(fqdn)
hostname = fqdn.split('.')[0]

desc = {'service': 'Assistive Technology Service', 'version': '1.0.0'}
info = ServiceInfo('_assistive-tech._udp.local.',
                    hostname + ' Assistive Technology Server._assistive-tech._udp.local.',
                    socket.inet_aton(ip_addr), PORT, 0, 0,
                    desc, hostname + '.local.')
try:
    zeroconf.register_service(info)
    print(" > Zeroconf service " + str(desc) + " registered:\n" + str(info))

    while True:
        message, address = serverSocket.recvfrom(1024)
        string = message.decode('utf-8')
        print(" > Received: " + message.decode('utf-8') + " from " + str(address))
        serverSocket.sendto("astv_ack".encode('utf-8'), address)
        if string == "astv_discover":
            returnMessage = "astv_shake:" + get_ip_address('en0')
            print(" > Discover call from client: " + str(address))
            print(" > Sending handshake: " + returnMessage + ", to address: " + str(address))
            serverSocket.sendto(returnMessage.encode('utf-8'), address)
            print(" > [" + str(datetime.datetime.now()) + "] New client connected <" + address[0] + ">")
        elif string == "astv_disconnect":
            break
finally:
    zeroconf.unregister_service(info)
    zeroconf.close()
    serverSocket.close()
