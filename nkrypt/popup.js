const recvFromParent = (e) => {
    if (e.origin !== 'https://docs.google.com') return;
    console.log(e);
    try {
        const msg = JSON.parse(e.data);
        if (msg.type === 'initialCipher') {
            document.getElementsByTagName('textarea')[0].value = msg.value;
        }
    } catch(err) {console.error(err);}
};

const encryptAndSend = async () => {
    const message = document.getElementsByTagName('textarea')[0].value;
    const password = document.getElementById('pwd').value;
    if (message.indexOf('-----BEGINNKRYPT-----') !== 0) {
        const ciphertext = await aesEncrypt(message, password);
        document.getElementsByTagName('textarea')[0].value = '-----BEGINNKRYPT-----\n' + ciphertext + '\n-----ENDNKRYPT-----';
    } else {
        const ciphertext = message
        .split('-----BEGINNKRYPT-----')[1]
        .split('-----ENDNKRYPT-----')[0]
        .replace(/(\r\n|\n|\r)/gm, "")
        .replace(/\s/g,'')
        .replace(/[^\x00-\x7F]/g, "");
        const cleartext = await aesDecrypt(ciphertext, password);
        document.getElementsByTagName('textarea')[0].value = cleartext;
    }
    //parent.postMessage(cipher, 'https://docs.google.com/');
};

window.addEventListener('message', recvFromParent, false);
document.getElementById('go').addEventListener('click', async () => encryptAndSend());

parent.postMessage('ready', 'https://docs.google.com/');