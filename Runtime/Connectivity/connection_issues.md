# Cross-Device Connection Issues & Fixes

## Problem Summary
The VR Resist networking system works when both apps run on the same computer but fails when connecting between different devices.

## Root Causes Identified

### 1. UDP Broadcast Limitations (ServerBroadcaster.cs)
- **Issue**: Uses `IPAddress.Broadcast` which only works within same subnet
- **Location**: Line with `IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);`
- **Impact**: Broadcast packets don't cross network boundaries/routers

### 2. Hardcoded IP Addresses (ConnectionPrompt.cs)
- **Issue**: Contains hardcoded IPs that may not be reachable from other devices
- **Locations**: 
  - Default IP: `"10.51.14.177:8125"`
  - Saved IP: `"192.168.178.39"`
- **Impact**: These specific IPs may not exist on user's network

### 3. IP Discovery Fallback Issues
- **Issue**: Both TCPServer.cs and WebSocketClient.cs fall back to `"127.0.0.1"` 
- **Impact**: Returns localhost instead of actual network IP when discovery fails

### 4. Network Interface Selection
- **Issue**: No mechanism to choose correct network adapter
- **Impact**: May discover wrong IP address on multi-homed systems

## TODO List - Priority Order

### High Priority (Critical Fixes)

- [x] **Fix IP Discovery Logic**
  - [x] Modify `GetCurrentIP()` method to prioritize non-loopback addresses - **TCPServer.cs**
  - [x] Modify `GetLocalIp()` method to prioritize non-loopback addresses - **WebSocketClient.cs**
  - [x] Filter out link-local addresses (169.254.x.x) - **TCPServer.cs & WebSocketClient.cs**
  - [x] Prefer IPv4 addresses over IPv6 for compatibility - **TCPServer.cs & WebSocketClient.cs**

- [x] **Replace UDP Broadcast with Better Discovery**
  - [x] Option A: Implement multicast discovery (224.0.0.0/4 range) - **ServerBroadcaster.cs**
  - [x] Option B: Add direct IP targeting option - **ServerBroadcaster.cs & ClientDiscovery.cs**
  - [x] Option C: Implement both for fallback support - **ServerBroadcaster.cs & ClientDiscovery.cs**

- [x] **Remove Hardcoded IP Addresses**
  - [x] Remove hardcoded `"10.51.14.177:8125"` from default input field - **ConnectionPrompt.cs**
  - [x] Remove hardcoded `"192.168.178.39"` from save logic - **ConnectionPrompt.cs**
  - [x] Use discovered IP addresses instead of hardcoded values - **ConnectionPrompt.cs**

### Medium Priority (User Experience)

- [ ] **Add Network Interface Selection UI**
  - [ ] Enumerate available network interfaces - **ConnectionPrompt.cs**
  - [ ] Allow user to select correct adapter - **ConnectionPrompt.cs**
  - [ ] Display IP addresses for each interface - **ConnectionPrompt.cs**
  - [ ] Save user's preferred interface choice - **ConnectionPrompt.cs**

- [ ] **Improve Connection Diagnostics**
  - [ ] Add connection status indicators - **ConnectionPrompt.cs**
  - [ ] Show discovered IP addresses in UI - **ConnectionPrompt.cs**
  - [ ] Add ping/connectivity test functionality - **WebSocketClient.cs**
  - [ ] Display detailed error messages for failed connections - **ConnectionPrompt.cs & WebSocketClient.cs**

- [ ] **Add Manual IP Override**
  - [ ] Provide UI field for manual server IP entry - **ConnectionPrompt.cs**
  - [ ] Validate IP address format - **ConnectionPrompt.cs**
  - [ ] Test connectivity before attempting full connection - **WebSocketClient.cs**
  - [ ] Save working IP addresses for future use - **ConnectionPrompt.cs**

### Low Priority (Robustness)

- [ ] **Implement Connection Retry Logic**
  - [ ] Automatic retry with different network interfaces - **WebSocketClient.cs**
  - [ ] Exponential backoff for failed connections - **WebSocketClient.cs**
  - [ ] Timeout handling for discovery process - **ClientDiscovery.cs**

- [ ] **Add Network Configuration Validation**
  - [ ] Check if required ports are available - **TCPServer.cs & WebSocketClient.cs**
  - [ ] Detect firewall blocking - **TCPServer.cs & WebSocketClient.cs**
  - [ ] Provide setup instructions for network configuration - **ConnectionPrompt.cs**

- [ ] **Enhance Security**
  - [ ] Add basic authentication/pairing mechanism - **TCPServer.cs & WebSocketClient.cs**
  - [ ] Implement connection encryption - **TCPServer.cs & WebSocketClient.cs**
  - [ ] Add device identification/naming - **TCPServer.cs & WebSocketClient.cs**

## Network Configuration Requirements

### Firewall Rules Needed
- [ ] **UDP Port 7778** - For server discovery broadcasts
- [ ] **TCP Port 8125** - For main communication channel

### Router/Network Setup
- [ ] Ensure devices are on same subnet OR
- [ ] Configure port forwarding if crossing subnets
- [ ] Enable UPnP if available for automatic port management

## Testing Checklist

### Same Network Testing
- [ ] Test on same WiFi network
- [ ] Test on same wired network
- [ ] Test mixed WiFi/wired setup

### Different Network Testing
- [ ] Test across different subnets
- [ ] Test with VPN connections
- [ ] Test with mobile hotspot

### Error Condition Testing
- [ ] Test with firewall enabled
- [ ] Test with multiple network adapters
- [ ] Test connection recovery after network changes

## Implementation Notes

### Code Files to Modify
1. **ServerBroadcaster.cs** - Discovery mechanism
2. **ClientDiscovery.cs** - Client-side discovery
3. **TCPServer.cs** - IP discovery method
4. **WebSocketClient.cs** - IP discovery method
5. **ConnectionPrompt.cs** - Remove hardcoded IPs, add UI improvements

### Backward Compatibility
- Maintain existing message protocols
- Keep current port numbers unless conflicts arise
- Ensure local (same-machine) connections continue working

## Success Criteria
- [ ] Connection works between devices on same network
- [ ] Connection works across different subnets (with proper network config)
- [ ] Clear error messages when connection fails
- [ ] Automatic discovery works reliably
- [ ] Manual connection option available as fallback
- [ ] No hardcoded IP addresses in production code
