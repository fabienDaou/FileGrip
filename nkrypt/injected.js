/*
chrome.runtime.onMessage.addListener(function(msg, sender){
    if(msg == "toggle_nkcrpt"){
        toggle();
    }
})
*/
var iframe = document.createElement('iframe'); 
iframe.style.background = "lightgreen";
iframe.style.height = "70%";
iframe.style.width = "400px";
iframe.style.position = "fixed";
iframe.style.top = "20%";
iframe.style.right = "10%";
iframe.style.zIndex = "9000000000000000000";
iframe.style.border = "1px";
iframe.src = chrome.runtime.getURL("popup.html");

const recvFromFrame = (e) => {
    if (e.origin.indexOf('chrome-extension') !== 0) return;
    if (e.data === 'ready') {
        try {
            let ciphertext = document.getElementById('docs-editor-container').textContent;
            ciphertext = ciphertext.split('-----BEGINNKRYPT-----')[1].split('-----ENDNKRYPT-----')[0];
            iframe.contentWindow.postMessage(JSON.stringify({
                type: 'initialCipher',
                value: '-----BEGINNKRYPT-----\n' + ciphertext + '\n-----ENDNKRYPT-----'
            }), e.origin);
        } catch (e) {console.error(e);}
    } else {
        writeDoc(e.data);
    }
};
window.addEventListener('message', recvFromFrame, false);
document.body.appendChild(iframe);

const writeDoc = (contents) => {
    let initspan, endspan;
    for (let span of document.getElementsByTagName('span')) {
        if (span.textContent.indexOf('-----BEGINNKRYPT-----') !== -1 && !initspan) {
            initspan = span;
        }
        if (initspan && !endspan) {
            span.textContent = '';
        }
        if (span.textContent.indexOf('-----ENDNKRYPT-----') === -1 && !endspan) {
            endspan = span; 
        }
    }
    initspan.textContent = '-----BEGINNKRYPT-----' + contents + '-----ENDNKRYPT-----';
};