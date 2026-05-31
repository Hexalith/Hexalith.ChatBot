window.HexalithChatBot = window.HexalithChatBot || {};
window.HexalithChatBot.focusElementById = (id) => {
    if (!id) {
        return;
    }

    const target = document.getElementById(id);
    if (target && typeof target.focus === "function") {
        target.focus();
    }
};
