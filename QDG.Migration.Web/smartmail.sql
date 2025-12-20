/*
 Navicat Premium Data Transfer

 Source Server         : localhost
 Source Server Type    : MongoDB
 Source Server Version : 80004
 Source Host           : localhost:27017
 Source Schema         : smartmail

 Target Server Type    : MongoDB
 Target Server Version : 80004
 File Encoding         : 65001

 Date: 19/12/2025 10:56:47
*/


// ----------------------------
// Collection structure for audit_logs
// ----------------------------
db.getCollection("audit_logs").drop();
db.createCollection("audit_logs");
db.getCollection("audit_logs").createIndex({
    timestamp: NumberInt("-1")
}, {
    name: "timestamp_-1"
});
db.getCollection("audit_logs").createIndex({
    "trace_id": NumberInt("1")
}, {
    name: "trace_id_1"
});
db.getCollection("audit_logs").createIndex({
    timestamp: NumberInt("1")
}, {
    name: "timestamp_1"
});

// ----------------------------
// Documents of audit_logs
// ----------------------------

// ----------------------------
// Collection structure for email_summaries
// ----------------------------
db.getCollection("email_summaries").drop();
db.createCollection("email_summaries");
db.getCollection("email_summaries").createIndex({
    messageId: NumberInt("1")
}, {
    name: "messageId_1",
    unique: true
});

// ----------------------------
// Documents of email_summaries
// ----------------------------

// ----------------------------
// Collection structure for emails
// ----------------------------
db.getCollection("emails").drop();
db.createCollection("emails");
db.getCollection("emails").createIndex({
    messageId: NumberInt("1")
}, {
    name: "messageId_1",
    unique: true
});

// ----------------------------
// Documents of emails
// ----------------------------

// ----------------------------
// Collection structure for metrics
// ----------------------------
db.getCollection("metrics").drop();
db.createCollection("metrics");
db.getCollection("metrics").createIndex({
    name: NumberInt("1"),
    timestamp: NumberInt("-1")
}, {
    name: "name_1_timestamp_-1"
});
db.getCollection("metrics").createIndex({
    name: NumberInt("1"),
    timestamp: NumberInt("1")
}, {
    name: "name_1_timestamp_1"
});

// ----------------------------
// Documents of metrics
// ----------------------------

// ----------------------------
// Collection structure for thread_summaries
// ----------------------------
db.getCollection("thread_summaries").drop();
db.createCollection("thread_summaries");
db.getCollection("thread_summaries").createIndex({
    "conversation_id": NumberInt("1")
}, {
    name: "conversation_id_1",
    unique: true
});
db.getCollection("thread_summaries").createIndex({
    "updated_at": NumberInt("-1")
}, {
    name: "updated_at_-1"
});
db.getCollection("thread_summaries").createIndex({
    threadId: NumberInt("1")
}, {
    name: "threadId_1",
    unique: true
});

// ----------------------------
// Documents of thread_summaries
// ----------------------------
