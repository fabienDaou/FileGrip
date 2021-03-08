chrome.runtime.onInstalled.addListener(async () => {
    console.log('INSTALLED');
});

/*
chrome.action.onClicked.addListener((tab) => {
    chrome.tabs.sendMessage(tab.id, 'toggle_nkcrpt');
});
*/