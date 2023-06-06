/*
* 作业请求队列由单个读写锁保护，多个工作线程获取单个主线程分配给它们的作业
*/
#include "apue.h"
#include <pthread.h>

struct job {
    struct job *j_next;
    struct job *j_prev;
    pthread_t j_id; // 哪个线程处理这个作业
    /* 省略其它属性 ... */
};

struct queue {
    struct job *q_head;
    struct job *q_tail;
    pthread_rwlock_t q_lock;
}

/*
* 初始化一个作业队列
*/
int queue_init(struct queue *qp)
{
    int err;
    qp->q_head = NULL;
    qp->q_tail = NULL;
    err = pthread_rwlock_init(&qp->q_lock, NULL); // 初始化读写锁
    if (err != 0)
        return(err);
    /* 继续初始化其它字段 ... */
    return 0;
}

/*
* 在作业队列头部插入一个作业
*/
void job_insert(struct queue *qp, struct job *jp)
{
    pthread_rwlock_wrlock(&qp->q_lock); // 获取写锁
    jp->j_next = qp->q_head; // 插入到队列头部
    jp->j_prev = NULL;
    if (qp->q_head != NULL)
        qp->q_head->j_prev = jp;
    else
        qp->q_tail = jp; // 队列为空
    qp->q_head = jp;
    pthread_rwlock_unlock(&qp->q_lock); // 释放写锁
}

/*
* 在作业队列尾部插入一个作业
*/
void job_append(struct queue *qp, struct job *jp)
{
    pthread_rwlock_wrlock(&qp->q_lock); // 获取写锁
    jp->j_next = NULL; // 插入到队列尾部
    jp->j_prev = qp->q_tail;
    if (qp->q_tail != NULL)
        qp->q_tail->j_next = jp;
    else
        qp->q_head = jp; // 队列为空
    qp->q_tail = jp;
    pthread_rwlock_unlock(&qp->q_lock); // 释放写锁
}

/*
* 从作业队列中删除一个作业
*/
void job_remove(struct queue *qp, struct job *jp)
{
    pthread_rwlock_wrlock(&qp->q_lock); // 获取写锁
    if (jp == qp->q_head) {
        qp->q_head = jp->j_next;
        if (qp->q_tail == jp)
            qp->q_tail = NULL;
        else
            jp->j_next->j_prev = jp->j_prev;
    } else if (jp == qp->q_tail) {
        qp->q_tail = jp->j_prev;
        jp->j_prev->j_next = jp->j_next;
    } else {
        jp->j_prev->j_next = jp->j_next;
        jp->j_next->j_prev = jp->j_prev;
    }
    pthread_rwlock_unlock(&qp->q_lock); // 释放写锁
}

/*
* 查找作业 ID 为 id 的作业
*/
struct job *job_find(struct queue *qp, pthread_t id)
{
    struct job *jp;
    if (pthread_rwlock_rdlock(&qp->q_lock) != 0) // 获取读锁
        return(NULL);
    for (jp = qp->q_head; jp != NULL; jp = jp->j_next)
        if (pthread_equal(jp->j_id, id))
            break;
    pthread_rwlock_unlock(&qp->q_lock); // 释放读锁
    return(jp);
}
