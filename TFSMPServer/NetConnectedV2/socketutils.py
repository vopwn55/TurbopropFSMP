import zlib
messagediv = "\x1C"
partdiv = "\x1D"

errormsg = "\uE000"
normalmsg = "\uE001"
querymsg = "\uE002"

GLOBALFLAGS = {
}

LastDefaultID = 0
ZLibCompression = False

def decompress(data):
    if ZLibCompression:
        return zlib.decompress(data)
    return data

def compress(data):
    if ZLibCompression:
        return zlib.compress(data)
    return data

def SocketReceiveAll(socketconn, messagediv):
    receiveddata = bytearray()
    messagediv_bytes = messagediv.encode()

    while True:
        data = socketconn.recv(1024)  # Adjust the buffer size as needed
        if not data:
            break  # Connection closed by the client

        receiveddata += data

        # Check if messagediv is in the receiveddata
        index = receiveddata.find(messagediv_bytes)
        if index != -1:
            receiveddata = receiveddata[:index]  # Exclude messagediv and everything after
            break

    return receiveddata

def read_until_character(sock, character=b"\x1C", max_size=10240):
    data = b""
    total_received = 0
    while True:
        chunk = sock.recv(min(1024, max_size - total_received))  # Adjust buffer size as needed
        if not chunk or total_received >= max_size:
            break
        data += chunk
        total_received += len(chunk)
        if character in chunk:
            break
    return data.split(character)[0]


def DecompressBracketIntoDict(self, bracketinput):
        """Converts a bracket into a dictionary for ServiceCollector"""
        if not bracketinput:
            return {}
        
        parts = bracketinput.split(partdiv)
        if len(parts) % 3 != 0:
            return {}

        decompressed_dict = {}
        for i in range(0, len(parts), 3):
            compressed_length = int(parts[i].decode())
            service_name = parts[i + 1].decode()
            compressed_data = parts[i + 2]
            decompressed_data = decompress(compressed_data)
            decompressed_dict[service_name] = decompressed_data.decode()
        
        return decompressed_dict

def CompressDictIntoBracket(self, dictinput):
        """Converts a dictionary from ServiceCollector into a bracket"""
        if type(dictinput) != dict:
            return b""
        bracket = bytearray()
        for servicename, data in dictinput.items():
            data = str(data).encode()
            CompressedData = compress(data)
            CompressedLength = len(CompressedData)
            bracket += str(CompressedLength).encode() + partdiv.encode() + servicename.encode() + partdiv.encode() + CompressedData
        return bracket